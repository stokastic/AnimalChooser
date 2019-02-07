using Microsoft.Xna.Framework;
using Paritee.StardewValleyAPI.Buildings.AnimalHouses;
using Paritee.StardewValleyAPI.Buildings.AnimalShop;
using Paritee.StardewValleyAPI.Buildings.AnimalShop.FarmAnimals;
using Paritee.StardewValleyAPI.FarmAnimals;
using Paritee.StardewValleyAPI.FarmAnimals.Variations;
using Paritee.StardewValleyAPI.Players;
using Paritee.StardewValleyAPI.Utilities;
using StardewModdingAPI;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimalChooser.Integrations
{
    class PariteeIntegration
    {
        public const string BetterFarmAnimalVarietyModId = "Paritee.BetterFarmAnimalVariety";
        private const string BetterFarmAnimalVarietyApiVersion = "2";
        private const int DataFarmAnimalsPriceIndex = 24;
        private const char DataFarmAnimalsDelimiter = '/';

        public enum Chickens
        {
            Normal,
            Void,
            Blue
        }

        public const string VanillaChicken = "Chicken";
        public const string VanillaDairyCow = "Dairy Cow";
        public const string VanillaGoat = "Goat";
        public const string VanillaDuck = "Duck";
        public const string VanillaSheep = "Sheep";
        public const string VanillaRabbit = "Rabbit";
        public const string VanillaPig = "Pig";

        private readonly IModHelper Helper;
        private readonly object Api;
        public readonly bool IsBetterFarmAnimalVarietyEnabled;
        private readonly FarmAnimalsData FarmAnimalsData;

        public AnimalShop AnimalShop;
        private Player Player;

        public PariteeIntegration(IModHelper helper)
        {
            this.Helper = helper;
            this.Api = helper.ModRegistry.GetApi(BetterFarmAnimalVarietyModId);
            this.IsBetterFarmAnimalVarietyEnabled = this.IsApiLoaded() ? this.Helper.Reflection.GetMethod(this.Api, "IsEnabled").Invoke<bool>(BetterFarmAnimalVarietyApiVersion) : false;
            this.FarmAnimalsData = new FarmAnimalsData();
        }

        public bool IsApiLoaded()
        {
            return this.Api != null;
        }

        public void Setup(Player player)
        {
            this.Player = player;

            if (this.IsBetterFarmAnimalVarietyEnabled)
            {
                // Use BFAV's custom configuration values
                this.SetupBetterFarmAnimalVarietyAnimalShop();
            }
            else
            {
                // Use the Data/FarmAnimals
                this.SetupDefaultAnimalShop();
            }
        }

        private void SetupDefaultAnimalShop()
        {
            BlueConfig blueConfig = new BlueConfig(this.Player.HasSeenEvent(BlueVariation.EVENT_ID));
            BlueVariation blueFarmAnimals = new BlueVariation(blueConfig);

            VoidConfig voidConfig = new VoidConfig(VoidConfig.InShop.Never, this.Player.HasCompletedQuest(VoidVariation.QUEST_ID));
            VoidVariation voidFarmAnimals = new VoidVariation(voidConfig);

            List<FarmAnimalForPurchase> farmAnimalsForPurchase = this.GetVanillaFarmAnimalsForPurchase();

            StockConfig stockConfig = new StockConfig(farmAnimalsForPurchase, blueFarmAnimals, voidFarmAnimals);
            Stock stock = new Stock(stockConfig);

            this.AnimalShop = new AnimalShop(stock);
        }

        private void SetupBetterFarmAnimalVarietyAnimalShop()
        {
            BlueVariation blueFarmAnimals = this.Helper.Reflection.GetMethod(this.Api, "GetBlueFarmAnimals").Invoke<BlueVariation>(BetterFarmAnimalVarietyApiVersion, this.Player);
            VoidVariation voidFarmAnimals = this.Helper.Reflection.GetMethod(this.Api, "GetVoidFarmAnimals").Invoke<VoidVariation>(BetterFarmAnimalVarietyApiVersion, this.Player);

            this.AnimalShop = this.Helper.Reflection.GetMethod(this.Api, "GetAnimalShop").Invoke<AnimalShop>(BetterFarmAnimalVarietyApiVersion, Game1.getFarm(), blueFarmAnimals, voidFarmAnimals);
        }

        public FarmAnimalForPurchase GetFarmAnimalForPurchase(string name)
        {
            return this.AnimalShop.FarmAnimalStock
                .FarmAnimalsForPurchase.Find(i => i.Name.Equals(name));
        }

        public string GetTypeAtIndex(string name, int index)
        {
            return this.GetFarmAnimalForPurchase(name).FarmAnimalTypes.ElementAt(index);
        }

        public Paritee.StardewValleyAPI.FarmAnimals.FarmAnimal GetFarmAnimal(string type)
        {
            return this.GetFarmAnimal(type, this.Player.GetNewID(), this.Player.MyID);
        }

        public Paritee.StardewValleyAPI.FarmAnimals.FarmAnimal GetFarmAnimal(string type, long myId, long ownerId)
        {
            return new Paritee.StardewValleyAPI.FarmAnimals.FarmAnimal(type, myId, ownerId);
        }

        public bool IsCoopDweller(Paritee.StardewValleyAPI.FarmAnimals.FarmAnimal animal)
        {
            return animal.buildingTypeILiveIn.Value.Equals(Coop.COOP);
        }

        public bool CanPurchaseVoidFromShop()
        {
            return this.AnimalShop.FarmAnimalStock.VoidFarmAnimals.CanPurchaseFromShop();
        }

        public Chickens DetermineChickenVariation(string type)
        {
            if (this.IsChickenVariation(type, PariteeIntegration.Chickens.Blue))
            {
                return PariteeIntegration.Chickens.Blue;
            }

            if (this.IsChickenVariation(type, PariteeIntegration.Chickens.Void))
            {
                return PariteeIntegration.Chickens.Void;
            }

            return PariteeIntegration.Chickens.Normal;
        }

        private bool IsChickenVariation(string type, Chickens variation)
        {
            Paritee.StardewValleyAPI.FarmAnimals.Type.Base @base = Paritee.StardewValleyAPI.FarmAnimals.Type.Base.Chicken;

            return variation.Equals(Chickens.Blue) ? this.IsBlueVariation(type, @base) : this.IsVoidVariation(type, @base);
        }

        private bool IsBlueVariation(string type, Paritee.StardewValleyAPI.FarmAnimals.Type.Base @base)
        {
            return type.Equals(this.AnimalShop.FarmAnimalStock.BlueFarmAnimals.ApplyPrefix(Enums.GetValue(@base)));
        }

        private bool IsVoidVariation(string type, Paritee.StardewValleyAPI.FarmAnimals.Type.Base @base)
        {
            return type.Equals(this.AnimalShop.FarmAnimalStock.VoidFarmAnimals.ApplyPrefix(Enums.GetValue(@base)));
        }

        public void DrawFarmAnimal(Paritee.StardewValleyAPI.FarmAnimals.FarmAnimal animal, int x, int y, int w, int h)
        {
            Sprite sprite = new Sprite(animal.type.Value);
            AnimatedSprite animatedSprite = new AnimatedSprite(sprite.FormatAdultFilePath(), animal.Sprite.CurrentFrame, animal.Sprite.SpriteWidth, animal.Sprite.SpriteHeight);

            Game1.spriteBatch.Draw(animatedSprite.Texture, new Rectangle(x, y, w, h), animal.frontBackSourceRect.Value, Color.White);
        }

        private List<FarmAnimalForPurchase> GetVanillaFarmAnimalsForPurchase()
        {
            string barn = Barn.FormatBuilding(Barn.BARN, Barn.Size.Small);
            string bigBarn = Barn.FormatBuilding(Barn.BARN, Barn.Size.Big);
            string deluxeBarn = Barn.FormatBuilding(Barn.BARN, Barn.Size.Deluxe);
            string coop = Coop.FormatBuilding(Coop.COOP, Coop.Size.Small);
            string bigCoop = Coop.FormatBuilding(Coop.COOP, Coop.Size.Big);
            string deluxeCoop = Coop.FormatBuilding(Coop.COOP, Coop.Size.Deluxe);

            Dictionary<string, int> prices = this.GetVanillaFarmAnimalPrices();
            Dictionary<string, string> displayNames = this.GetVanillaFarmAnimalDisplayNames();
            Dictionary<string, string> descriptions = this.GetVanillaFarmAnimalDescriptions();

            return new List<FarmAnimalForPurchase>()
            {
                new FarmAnimalForPurchase(VanillaChicken, displayNames[VanillaChicken], descriptions[VanillaChicken], prices[VanillaChicken], new List<string>() { coop, bigCoop, deluxeCoop }, new List<string>() { "White Chicken", "Brown Chicken" }),
                new FarmAnimalForPurchase(VanillaDairyCow, displayNames[VanillaDairyCow], descriptions[VanillaDairyCow], prices[VanillaDairyCow], new List<string>() { barn, bigBarn, deluxeBarn }, new List<string>() { "White Cow", "Brown Cow" }),
                new FarmAnimalForPurchase(VanillaGoat, displayNames[VanillaGoat], descriptions[VanillaGoat], prices[VanillaGoat], new List<string>() { bigBarn, deluxeBarn }, new List<string>() { VanillaGoat }),
                new FarmAnimalForPurchase(VanillaDuck, displayNames[VanillaDuck], descriptions[VanillaDuck], prices[VanillaDuck], new List<string>() { bigCoop, deluxeCoop }, new List<string>() { VanillaDuck }),
                new FarmAnimalForPurchase(VanillaSheep, displayNames[VanillaSheep], descriptions[VanillaSheep], prices[VanillaSheep], new List<string>() { deluxeBarn }, new List<string>() { VanillaSheep }),
                new FarmAnimalForPurchase(VanillaRabbit, displayNames[VanillaRabbit], descriptions[VanillaRabbit], prices[VanillaRabbit], new List<string>() { deluxeCoop }, new List<string>() { VanillaRabbit }),
                new FarmAnimalForPurchase(VanillaPig, displayNames[VanillaPig], descriptions[VanillaPig], prices[VanillaPig], new List<string>() { deluxeBarn }, new List<string>() { VanillaPig }),
            };
        }

        private Dictionary<string, int> GetVanillaFarmAnimalPrices()
        {
            Dictionary<string, string> entries = this.FarmAnimalsData.GetEntries();

            WhiteVariation whiteVariation = new WhiteVariation();
            string whiteChicken = whiteVariation.ApplyPrefix(Enums.GetValue(Paritee.StardewValleyAPI.FarmAnimals.Type.Base.Chicken));
            string whiteCow = whiteVariation.ApplyPrefix(Enums.GetValue(Paritee.StardewValleyAPI.FarmAnimals.Type.Base.Cow));

            return new Dictionary<string, int>()
            {
                { VanillaChicken, Int32.Parse(entries[whiteChicken].Split(DataFarmAnimalsDelimiter)[DataFarmAnimalsPriceIndex]) },
                { VanillaDairyCow, Int32.Parse(entries[whiteCow].Split(DataFarmAnimalsDelimiter)[DataFarmAnimalsPriceIndex]) },
                { VanillaGoat, Int32.Parse(entries[VanillaGoat].Split(DataFarmAnimalsDelimiter)[DataFarmAnimalsPriceIndex]) },
                { VanillaDuck, Int32.Parse(entries[VanillaDuck].Split(DataFarmAnimalsDelimiter)[DataFarmAnimalsPriceIndex]) },
                { VanillaSheep, Int32.Parse(entries[VanillaSheep].Split(DataFarmAnimalsDelimiter)[DataFarmAnimalsPriceIndex]) },
                { VanillaRabbit, Int32.Parse(entries[VanillaRabbit].Split(DataFarmAnimalsDelimiter)[DataFarmAnimalsPriceIndex]) },
                { VanillaPig, Int32.Parse(entries[VanillaPig].Split(DataFarmAnimalsDelimiter)[DataFarmAnimalsPriceIndex]) },
            };
        }

        private Dictionary<string, string> GetVanillaFarmAnimalDisplayNames()
        {
            return new Dictionary<string, string>()
            {
                { VanillaChicken, Game1.content.LoadString("Strings\\StringsFromCSFiles:Utility.cs.5922") },
                { VanillaDairyCow, Game1.content.LoadString("Strings\\StringsFromCSFiles:Utility.cs.5927") },
                { VanillaGoat, Game1.content.LoadString("Strings\\StringsFromCSFiles:Utility.cs.5933") },
                { VanillaDuck, Game1.content.LoadString("Strings\\StringsFromCSFiles:Utility.cs.5937") },
                { VanillaSheep, Game1.content.LoadString("Strings\\StringsFromCSFiles:Utility.cs.5942") },
                { VanillaRabbit, Game1.content.LoadString("Strings\\StringsFromCSFiles:Utility.cs.5945") },
                { VanillaPig, Game1.content.LoadString("Strings\\StringsFromCSFiles:Utility.cs.5948") },
            };
        }

        private Dictionary<string, string> GetVanillaFarmAnimalDescriptions()
        {
            return new Dictionary<string, string>()
            {
                { VanillaChicken, Game1.content.LoadString("Strings\\StringsFromCSFiles:PurchaseAnimalsMenu.cs.11334") },
                { VanillaDairyCow, Game1.content.LoadString("Strings\\StringsFromCSFiles:PurchaseAnimalsMenu.cs.11343") },
                { VanillaGoat, Game1.content.LoadString("Strings\\StringsFromCSFiles:PurchaseAnimalsMenu.cs.11349") },
                { VanillaDuck, Game1.content.LoadString("Strings\\StringsFromCSFiles:PurchaseAnimalsMenu.cs.11337") },
                { VanillaSheep, Game1.content.LoadString("Strings\\StringsFromCSFiles:PurchaseAnimalsMenu.cs.11352") },
                { VanillaRabbit, Game1.content.LoadString("Strings\\StringsFromCSFiles:PurchaseAnimalsMenu.cs.11340") },
                { VanillaPig, Game1.content.LoadString("Strings\\StringsFromCSFiles:PurchaseAnimalsMenu.cs.11346") },
            };
        }
    }
}
