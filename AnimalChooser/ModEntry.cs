using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnimalChooser.Integrations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;

namespace AnimalChooser
{
    public class ModEntry : Mod {

        private ModConfig Config;

        private FarmAnimal currentAnimal = null;
        private int heartLevel = 0;

        private Dictionary<string, string> animalData;
        private Texture2D heartFullTexture;
        private Texture2D heartEmptyTexture;

        private Dictionary<string, List<string>> categories;

        private const string BetterFarmAnimalVarietyUniqueId = "Paritee.BetterFarmAnimalVariety";

        public override void Entry(IModHelper helper) {

            Config = Helper.ReadConfig<ModConfig>();

            heartFullTexture = helper.Content.Load<Texture2D>("Assets/heartFull.png");
            heartEmptyTexture = helper.Content.Load<Texture2D>("Assets/heartEmpty.png");
            
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.MouseWheelScrolled += OnMouseWheelScrolled;
            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.Display.Rendered += OnRendered;
            helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdateTicked;
        }

        private void RefreshCategories()
        {
            animalData = Game1.content.Load<Dictionary<string, string>>("Data\\FarmAnimals");

            // Integrate with Better Farm Animal Variety (BFAV) >= 3.x
            IBetterFarmAnimalVarietyApi api = this.Helper.ModRegistry.GetApi<IBetterFarmAnimalVarietyApi>(BetterFarmAnimalVarietyUniqueId);

            categories = api != null 
                ? api.GetFarmAnimalCategories()
                : new Dictionary<string, List<string>>()
                {
                    { "Chickens", new List<string>() { "White Chicken", "Brown Chicken", "Void Chicken", "Blue Chicken" } },
                    { "Cows", new List<string>() { "White Cow", "Brown Cow" } },
                    { "Pigs", new List<string>() { "Pig" } },
                    { "Goats", new List<string>() { "Goat" } },
                    { "Sheep", new List<string>() { "Sheep" } },
                    { "Rabbits", new List<string>() { "Rabbit" } },
                    { "Ducks", new List<string>() { "Duck" } },
                };

            foreach (KeyValuePair<string, List<string>> entry in categories) {
                entry.Value.RemoveAll(str => !IsChickenTypeUnlocked(str));
            }

            Monitor.Log("Refreshed categories:\n" + string.Join("\n", categories.Select(kvp => $"- {kvp.Key}: {string.Join(", ", kvp.Value)}")), LogLevel.Trace);
        }

        /// <summary>Raised after the player scrolls the mouse wheel.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnMouseWheelScrolled(object sender, MouseWheelScrolledEventArgs e) {
            
            if (currentAnimal == null) {
                return;
            }

            if (e.Delta < 0 && heartLevel > 0) {
                heartLevel -= 1;
            } else if (e.Delta > 0 && heartLevel < 5) {
                heartLevel += 1;
            } else {
                return;
            }

            Game1.playSound("smallSelect");
        }

        /// <summary>Raised once per second after the game state is updated.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnOneSecondUpdateTicked(object sender, OneSecondUpdateTickedEventArgs e) {

            currentAnimal = null;

            if (Game1.globalFade) {
                return;
            }

            if (!(Game1.activeClickableMenu is PurchaseAnimalsMenu menu)) {
                return;
            }

            if (Helper.Reflection.GetField<bool>(menu, "namingAnimal").GetValue()) {
                return;
            }

            if (!Helper.Reflection.GetField<bool>(menu, "onFarm").GetValue()) {
                return;
            }

            FarmAnimal animal = Helper.Reflection.GetField<FarmAnimal>(menu, "animalBeingPurchased").GetValue();

            if (animal == null) {
                return;
            }

            currentAnimal = animal;
        }

        /// <summary>Raised after the game draws to the sprite patch in a draw tick, just before the final sprite batch is rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRendered(object sender, RenderedEventArgs e) {

            if (currentAnimal == null) {
                return;
            }

            if (Game1.globalFade) {
                return;
            }

            int dx = currentAnimal.frontBackSourceRect.Width * 2;
            int dy = Convert.ToInt32(16 * Math.Sin(0.05 * Game1.ticks)) + dx;
            int w = dx * 2;
            int h = w;

            dy += heartLevel > 0 ? 24 : 0;
            int mx = Game1.getMouseX();
            int my = Game1.getMouseY();
            
            Game1.spriteBatch.Draw(Helper.Content.Load<Texture2D>(Path.Combine("Animals", currentAnimal.type.Value), ContentSource.GameContent), new Rectangle(mx - dx, my - 64 - dy, w, h), currentAnimal.frontBackSourceRect.Value, Color.White);

            if (heartLevel > 0) {
                for (int i=0; i<5; i++) {
                    Game1.spriteBatch.Draw(i < heartLevel ? heartFullTexture : heartEmptyTexture, new Rectangle(mx - (19 - i * 8) * 4, my - 28, 28, 24), Color.White);
                }
            }
        }

        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnButtonPressed(object sender, ButtonPressedEventArgs e) {

            if (e.Button == SButton.Escape || e.Button == SButton.E) {
                currentAnimal = null;
            }

            if (Game1.activeClickableMenu is PurchaseAnimalsMenu menu) {

                FarmAnimal animal = Helper.Reflection.GetField<FarmAnimal>(Game1.activeClickableMenu, "animalBeingPurchased").GetValue();
                if (e.Button == SButton.MouseLeft) {

                    if (menu.doneNamingButton.containsPoint((int)e.Cursor.ScreenPixels.X, (int)e.Cursor.ScreenPixels.Y) ||
                        menu.okButton.containsPoint((int)e.Cursor.ScreenPixels.X, (int)e.Cursor.ScreenPixels.Y)) {
                        currentAnimal = null;
                    }

                    Building buildingAt = Game1.getFarm().getBuildingAt(new Vector2(e.Cursor.AbsolutePixels.X, e.Cursor.AbsolutePixels.Y) / 64);
                    if (buildingAt != null && buildingAt.buildingType.Value.Contains(animal.buildingTypeILiveIn.Value) && 
                        !(buildingAt.indoors.Value as AnimalHouse).isFull()) {
                        currentAnimal = null;
                    }
                }

                if (currentAnimal == null) {
                    return;
                }

                bool leftOrRight = false;
                int delta = 0;
                if (e.Button == SButton.Left) {
                    delta = -1;
                    leftOrRight = true;
                } else if (e.Button == SButton.Right) {
                    delta = 1;
                    leftOrRight = true;
                }

                if (leftOrRight) {
                    List<string> types = this.categories.First(kvp => kvp.Value.Contains(currentAnimal.type.Value)).Value;
                    int index = index = (delta + types.FindIndex(str => str == currentAnimal.type.Value)) % types.Count;

                    animal.type.Value = animal.displayType = types[index];
                }
            }
        }

        private bool IsChickenTypeUnlocked(string type) {
            switch (type) {
                case "Void Chicken":
                    Game1.player.basicShipped.TryGetValue(305, out int eggsShipped);
                    Game1.player.basicShipped.TryGetValue(308, out int mayoShipped);
                    return Game1.player.eventsSeen.Contains(942069) || Game1.player.hasRustyKey || Config.EnableVoidChickens || eggsShipped > 0 || mayoShipped > 0;
                case "Blue Chicken": return Game1.player.eventsSeen.Contains(3900074) || Config.EnableBlueChickens;
                default: return true;
            }
        }

        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnMenuChanged(object sender, MenuChangedEventArgs e) {

            if (e.NewMenu is PurchaseAnimalsMenu) {
                this.RefreshCategories();
            }

            if (e.OldMenu is PurchaseAnimalsMenu menu2) {
                FarmAnimal animal = Helper.Reflection.GetField<FarmAnimal>(menu2, "animalBeingPurchased").GetValue();

                if (animal != null && currentAnimal != null) {
                    string key = currentAnimal.type.Value;

                    animalData.TryGetValue(key, out string data);

                    if (data != null) {
                        string[] strArray = data.Split('/');
                        animal.type.Value = key;
                        animal.sound.Value = strArray[4].Equals("none") ? null : strArray[4];
                        animal.defaultProduceIndex.Value = Convert.ToInt32(strArray[2]);
                        animal.deluxeProduceIndex.Value = Convert.ToInt32(strArray[3]);
                        if (Config.AnimalStartsAsAdult) {
                            animal.age.Value = animal.ageWhenMature.Value;
                        }
                        animal.Sprite = new AnimatedSprite("Animals\\" + (animal.age.Value < animal.ageWhenMature.Value ? "Baby" : "") + ((key.Equals("Duck") && animal.age.Value < animal.ageWhenMature.Value) ? "White Chicken" : key), 0, Convert.ToInt32(strArray[16]), Convert.ToInt32(strArray[17]));
                        animal.price.Value = Convert.ToInt32(strArray[24]);
                        animal.friendshipTowardFarmer.Value = heartLevel * 200;
                        animal.fullnessDrain.Value = Convert.ToByte(strArray[20]);
                        animal.happinessDrain.Value = Convert.ToByte(strArray[21]);
                        animal.meatIndex.Value = Convert.ToInt32(strArray[23]);
                    } else {
                        Monitor.Log($"data is null - key: {key}", LogLevel.Info);
                    }
                }
            }

            currentAnimal = null;
        }
    }
}
