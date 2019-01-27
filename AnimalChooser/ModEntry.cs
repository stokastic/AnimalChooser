using Paritee.StardewValleyAPI.FarmAnimals;
using PariteeFarmAnimal = Paritee.StardewValleyAPI.FarmAnimals.FarmAnimal;
using PariteeVoid = Paritee.StardewValleyAPI.FarmAnimals.Variations.Void;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using Paritee.StardewValleyAPI.Buidlings.AnimalShop;
using Paritee.StardewValleyAPI.Players;
using Paritee.StardewValleyAPI.FarmAnimals.Variations;
using Paritee.StardewValleyAPI.Buidlings.AnimalShop.FarmAnimals;
using Paritee.StardewValleyAPI.Utilities;
using FarmAnimalsData = Paritee.StardewValleyAPI.FarmAnimals.Data;
using FarmAnimalsType = Paritee.StardewValleyAPI.FarmAnimals.Type;
using System.Linq;

namespace AnimalChooser
{
    public class ModEntry : Mod {

        public const string BETTER_FARM_ANIMAL_VARIETY_ID = "Paritee.BetterFarmAnimalVariety";
        private Player Player;
        private AnimalShop AnimalShop;

        private ModConfig Config;

        private bool choosingAnimal = false;
        private bool drawAnimal = false;
        private Stock.Name currentAnimal;
        private int heartLevel = 0;
        private int currentIndex = 0;
        
        private Texture2D heartFullTexture;
        private Texture2D heartEmptyTexture;

        private enum Chickens
        {
            Normal, 
            Void, 
            Blue
        }

        public override void Entry(IModHelper helper)
        {
            this.Config = Helper.ReadConfig<ModConfig>();

            this.heartFullTexture = helper.Content.Load<Texture2D>("Assets/heartFull.png");
            this.heartEmptyTexture = helper.Content.Load<Texture2D>("Assets/heartEmpty.png");

            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.MouseWheelScrolled += OnMouseWheelScrolled;
            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.Display.Rendered += OnRendered;
            helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdateTicked;
        }

        private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {
            this.Player = new Player(Game1.player, this.Helper);

            object api = this.Helper.ModRegistry.GetApi(ModEntry.BETTER_FARM_ANIMAL_VARIETY_ID);

            if (api == null)
            {
                // Use the Data/FarmAnimals
                this.AnimalShop = this.SetupDefaultAnimalShop(this.Player);
            }
            else
            {
                // Use BFAV's custom configuration values
                this.AnimalShop = this.Helper.Reflection.GetMethod(api, "GetAnimalShop").Invoke<AnimalShop>(this.Player);
            }

            // Implement AnimalChooser's config settings
            this.AnimalShop.FarmAnimalStock.Available[Stock.Name.Chicken] = this.SanitizeChickens(this.AnimalShop.FarmAnimalStock.Available[Stock.Name.Chicken]);
        }

        private AnimalShop SetupDefaultAnimalShop(Player player)
        {
            BlueConfig blueConfig = new BlueConfig(player.HasSeenEvent(Blue.EVENT_ID));
            Blue blueFarmAnimals = new Blue(blueConfig);

            VoidConfig voidConfig = new VoidConfig(VoidConfig.InShop.Never, player.HasCompletedQuest(PariteeVoid.QUEST_ID));
            PariteeVoid voidFarmAnimals = new PariteeVoid(voidConfig);

            StockConfig stockConfig = new StockConfig(blueFarmAnimals, voidFarmAnimals);
            Stock stock = new Stock(stockConfig);

            return new AnimalShop(stock);
        }

        private string[]  SanitizeChickens(string[] types)
        {
            List<string> sanitized = new List<string>();
            string baseType = Enums.GetValue(FarmAnimalsType.Base.Chicken);

            foreach (string type in types)
            {
                Chickens variation;

                if (type.Equals(this.AnimalShop.FarmAnimalStock.VoidFarmAnimals.ApplyPrefix(baseType)))
                    variation = Chickens.Void;
                    else if(type.Equals(this.AnimalShop.FarmAnimalStock.BlueFarmAnimals.ApplyPrefix(baseType)))
                    variation = Chickens.Blue;
                else
                    variation = Chickens.Normal;

                if (this.IsChickenTypeUnlocked(variation))
                    sanitized.Add(type);
            }

            return sanitized.ToArray<string>();
        }

        /// <summary>Raised after the player scrolls the mouse wheel.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnMouseWheelScrolled(object sender, MouseWheelScrolledEventArgs e)
        {
            if (!this.drawAnimal)
                return;

            if (e.Delta < 0 && this.heartLevel > 0)
                this.heartLevel -= 1;
            else if (e.Delta > 0 && this.heartLevel < 5)
                this.heartLevel += 1;
            else
                return;

            Game1.playSound("smallSelect");
        }

        /// <summary>Raised once per second after the game state is updated.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnOneSecondUpdateTicked(object sender, OneSecondUpdateTickedEventArgs e)
        {

            if (!this.choosingAnimal)
                return;

            PurchaseAnimalsMenu menu = Game1.activeClickableMenu as PurchaseAnimalsMenu;

            if (menu != null)
            {
                StardewValley.FarmAnimal animal = Helper.Reflection.GetField<StardewValley.FarmAnimal>(Game1.activeClickableMenu, "animalBeingPurchased").GetValue();

                if (animal != null)
                    animal.type.Value = this.AnimalShop.FarmAnimalStock.Available[currentAnimal][currentIndex];
            }

            this.drawAnimal = true;
        }

        /// <summary>Raised after the game draws to the sprite patch in a draw tick, just before the final sprite batch is rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRendered(object sender, RenderedEventArgs e)
        {
            if (!this.choosingAnimal)
                return;

            if (Game1.globalFade)
                return;

            int dy = Convert.ToInt32(16 * Math.Sin(0.05 * Game1.ticks));
            int dx;
            int w = 64;
            int h = 64;

            switch (this.currentAnimal)
            {
                case Stock.Name.Chicken:
                case Stock.Name.Duck:
                case Stock.Name.Rabbit:
                    dx = 24;
                    dy += 24;
                    break;
                case Stock.Name.DairyCow:
                case Stock.Name.Goat:
                case Stock.Name.Pig:
                case Stock.Name.Sheep:
                    dx = 64;
                    dy += 88;
                    w = 128;
                    h = 128;
                    break;
                default:
                    return;
            }

            dy += this.heartLevel > 0 ? 24 : 0;
            int mx = Game1.getMouseX();
            int my = Game1.getMouseY();

            string currentType = this.AnimalShop.FarmAnimalStock.Available[currentAnimal][currentIndex];
            PariteeFarmAnimal pariteeFarmAnimal = new PariteeFarmAnimal(currentType, this.Player.MyID, this.Player.GetNewID());
            pariteeFarmAnimal.BecomeAnAdult();
            Game1.spriteBatch.Draw(pariteeFarmAnimal.Sprite.Texture, new Rectangle(mx - dx, my - 64 - dy, w, h), pariteeFarmAnimal.frontBackSourceRect, Color.White);

            if (this.heartLevel > 0)
            {
                for (int i=0; i<5; i++)
                    Game1.spriteBatch.Draw(i < this.heartLevel ? this.heartFullTexture : this.heartEmptyTexture, new Rectangle(mx - (19 - i * 8) * 4, my - 28, 28, 24), Color.White);
            }
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (e.Button == SButton.Escape || e.Button == SButton.E)
            {
                this.choosingAnimal = false;
                this.drawAnimal = false;
            }

            PurchaseAnimalsMenu menu = Game1.activeClickableMenu as PurchaseAnimalsMenu;

            if (menu != null)
            {

                StardewValley.FarmAnimal animalBeingPurchased = Helper.Reflection.GetField<StardewValley.FarmAnimal>(Game1.activeClickableMenu, "animalBeingPurchased").GetValue();

                if (e.Button == SButton.MouseLeft)
                {
                    if (menu.doneNamingButton.containsPoint((int)e.Cursor.ScreenPixels.X, (int)e.Cursor.ScreenPixels.Y) || menu.okButton.containsPoint((int)e.Cursor.ScreenPixels.X, (int)e.Cursor.ScreenPixels.Y))
                    {
                        this.choosingAnimal = false;
                        this.drawAnimal = false;
                    }

                    Building buildingAt = Game1.getFarm().getBuildingAt(new Vector2(e.Cursor.AbsolutePixels.X, e.Cursor.AbsolutePixels.Y) / 64);

                    if (buildingAt != null && buildingAt.buildingType.Value.Contains(animalBeingPurchased.buildingTypeILiveIn.Value) && !(buildingAt.indoors.Value as AnimalHouse).isFull())
                    {
                        this.choosingAnimal = false;
                        this.drawAnimal = false;
                    }

                    foreach (ClickableTextureComponent component in menu.animalsToPurchase)
                    {
                        if (component.containsPoint((int)e.Cursor.ScreenPixels.X, (int)e.Cursor.ScreenPixels.Y))
                        {
                            string stockNameString = component.item.Name;

                            if (stockNameString != null)
                            {
                                this.choosingAnimal = true;
                                this.currentIndex = 0;
                                this.currentAnimal = this.AnimalShop.FarmAnimalStock.StringToName(stockNameString);
                                break;
                            }
                        }
                    }
                }

                if (!this.choosingAnimal)
                    return;

                bool leftOrRight = false;
                int delta = 0;

                if (e.Button == SButton.Left)
                {
                    delta = -1;
                    leftOrRight = true;
                }
                else if (e.Button == SButton.Right)
                {
                    delta = 1;
                    leftOrRight = true;
                }

                if (leftOrRight)
                {
                    int total = this.AnimalShop.FarmAnimalStock.Available[currentAnimal].Length;
                    this.currentIndex = (delta + currentIndex + total) % total;

                    string type = this.AnimalShop.FarmAnimalStock.Available[currentAnimal][currentIndex];
                    PariteeFarmAnimal pariteeFarmAnimal = new PariteeFarmAnimal(type, this.Player.GetNewID(), this.Player.MyID);
                    animalBeingPurchased.displayType = pariteeFarmAnimal.displayType;
                }
            }
        }

        private bool IsChickenTypeUnlocked(Chickens type)
        {
            switch (type)
            {
                case Chickens.Void:
                    int eggsShipped, mayoShipped;
                    Game1.player.basicShipped.TryGetValue(305, out eggsShipped);
                    Game1.player.basicShipped.TryGetValue(308, out mayoShipped);
                    return Game1.player.eventsSeen.Contains(942069) || Game1.player.hasRustyKey || this.Config.EnableVoidChickens || eggsShipped > 0 || mayoShipped > 0;
                case Chickens.Blue:
                    return Game1.player.eventsSeen.Contains(3900074) || this.Config.EnableBlueChickens;
                default:
                    return true;
            }
        }

        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            PurchaseAnimalsMenu menu2 = e.OldMenu as PurchaseAnimalsMenu;

            if (menu2 != null)
            {
                StardewValley.FarmAnimal sdvAnimalBeingPurchased = this.Helper.Reflection.GetField<StardewValley.FarmAnimal>(menu2, "animalBeingPurchased").GetValue();

                if (sdvAnimalBeingPurchased != null)
                {
                    string type = this.AnimalShop.FarmAnimalStock.Available[this.currentAnimal][this.currentIndex];

                    PariteeFarmAnimal pariteeFarmAnimal = new PariteeFarmAnimal(type, sdvAnimalBeingPurchased.myID, sdvAnimalBeingPurchased.ownerID);

                    pariteeFarmAnimal.SetFriendshipHearts(this.heartLevel);

                    if (Config.AnimalStartsAsAdult)
                        pariteeFarmAnimal.BecomeAnAdult();

                    sdvAnimalBeingPurchased.type.Value = pariteeFarmAnimal.type;
                    sdvAnimalBeingPurchased.displayType = pariteeFarmAnimal.displayType;
                    sdvAnimalBeingPurchased.sound.Value = pariteeFarmAnimal.sound;
                    sdvAnimalBeingPurchased.defaultProduceIndex.Value = pariteeFarmAnimal.defaultProduceIndex;
                    sdvAnimalBeingPurchased.deluxeProduceIndex.Value = pariteeFarmAnimal.deluxeProduceIndex;
                    sdvAnimalBeingPurchased.age.Value = pariteeFarmAnimal.age;
                    sdvAnimalBeingPurchased.Sprite = pariteeFarmAnimal.Sprite;
                    sdvAnimalBeingPurchased.price.Value = pariteeFarmAnimal.price;
                    sdvAnimalBeingPurchased.friendshipTowardFarmer.Value = pariteeFarmAnimal.friendshipTowardFarmer;
                    sdvAnimalBeingPurchased.fullnessDrain.Value = pariteeFarmAnimal.fullnessDrain;
                    sdvAnimalBeingPurchased.happinessDrain.Value = pariteeFarmAnimal.happinessDrain;
                    sdvAnimalBeingPurchased.meatIndex.Value = pariteeFarmAnimal.meatIndex;
                }
            }

            this.choosingAnimal = false;
            this.heartLevel = 0;
        }
    }
}
