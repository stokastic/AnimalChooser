﻿using System;
using System.Collections.Generic;
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

        private bool choosingAnimal = false;
        private bool drawAnimal = false;
        private Animal currentAnimal = Animal.None;
        private int heartLevel = 0;
        private int chickenIndex = 0;
        private int cowIndex = 0;
        
        private List<Texture2D> chickenTextures;
        private List<Texture2D> cowTextures;
        private List<Texture2D> pigTextures;
        private List<Texture2D> goatTextures;
        private List<Texture2D> sheepTextures;
        private List<Texture2D> rabbitTextures;
        private List<Texture2D> duckTextures;
        private Dictionary<string, string> animalData;
        private Texture2D heartFullTexture;
        private Texture2D heartEmptyTexture;

        private readonly List<string> chickens = new List<string>() {
            "White Chicken",
            "Brown Chicken",
            "Void Chicken",
            "Blue Chicken",
        };

        private readonly List<string> cows = new List<string>() {
            "White Cow",
            "Brown Cow",
        };

        private readonly List<string> pigs = new List<string>() { "Pig" };
        private readonly List<string> goats = new List<string>() { "Goat" };
        private readonly List<string> sheeps = new List<string>() { "Sheep" };
        private readonly List<string> rabbits = new List<string>() { "Rabbit" };
        private readonly List<string> ducks = new List<string>() { "Duck" };

        public override void Entry(IModHelper helper) {

            Config = Helper.ReadConfig<ModConfig>();

            chickenTextures = new List<Texture2D>() {
                helper.Content.Load<Texture2D>("Assets/White Chicken.png"),
                helper.Content.Load<Texture2D>("Assets/Brown Chicken.png"),
                helper.Content.Load<Texture2D>("Assets/Void Chicken.png"),
                helper.Content.Load<Texture2D>("Assets/Blue Chicken.png"),
            };

            cowTextures = new List<Texture2D>() {
                helper.Content.Load<Texture2D>("Assets/White Cow.png"),
                helper.Content.Load<Texture2D>("Assets/Brown Cow.png"),
            };

            pigTextures = new List<Texture2D>() { helper.Content.Load<Texture2D>("Assets/Pig.png"), };
            goatTextures = new List<Texture2D>() { helper.Content.Load<Texture2D>("Assets/Goat.png"), };
            sheepTextures = new List<Texture2D>() { helper.Content.Load<Texture2D>("Assets/Sheep.png"), };
            rabbitTextures = new List<Texture2D>() { helper.Content.Load<Texture2D>("Assets/Rabbit.png"), };
            duckTextures = new List<Texture2D>() { helper.Content.Load<Texture2D>("Assets/Duck.png"), };

            heartFullTexture = helper.Content.Load<Texture2D>("Assets/heartFull.png");
            heartEmptyTexture = helper.Content.Load<Texture2D>("Assets/heartEmpty.png");

            animalData = Game1.content.Load<Dictionary<string, string>>("Data\\FarmAnimals");

            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.MouseWheelScrolled += OnMouseWheelScrolled;
            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.Display.Rendered += OnRendered;
            helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdateTicked;
        }

        /// <summary>Raised after the player scrolls the mouse wheel.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnMouseWheelScrolled(object sender, MouseWheelScrolledEventArgs e) {
            
            if (!drawAnimal) {
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

            if (!choosingAnimal) {
                return;
            }

            PurchaseAnimalsMenu menu = Game1.activeClickableMenu as PurchaseAnimalsMenu;

            if (menu != null) {

                FarmAnimal animal = Helper.Reflection.GetField<FarmAnimal>(Game1.activeClickableMenu, "animalBeingPurchased").GetValue();

                if (animal != null) {
                    if (currentAnimal == Animal.Chicken) {
                        animal.type.Value = chickens[chickenIndex];
                    } else if (currentAnimal == Animal.Cow) {
                        animal.type.Value = cows[cowIndex];
                    }                    
                }
            }

            drawAnimal = true;
        }

        /// <summary>Raised after the game draws to the sprite patch in a draw tick, just before the final sprite batch is rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRendered(object sender, RenderedEventArgs e) {

            if (!choosingAnimal) {
                return;
            }

            if (Game1.globalFade) {
                return;
            }

            int dy = Convert.ToInt32(16 * Math.Sin(0.05 * Game1.ticks));
            int dx;
            int w = 64;
            int h = 64;
            switch (currentAnimal) {
                case Animal.Chicken:
                case Animal.Duck:
                case Animal.Rabbit:
                    dx = 24;
                    dy += 24;
                    break;
                case Animal.Cow:
                case Animal.Goat:
                case Animal.Pig:
                case Animal.Sheep:
                    dx = 64;
                    dy += 88;
                    w = 128;
                    h = 128;
                    break;
                default:
                    return;
            }
            dy += heartLevel > 0 ? 24 : 0;
            int mx = Game1.getMouseX();
            int my = Game1.getMouseY();
            Texture2D texture = chickenTextures[0];
            switch (currentAnimal) {
                case Animal.Chicken: texture = chickenTextures[chickenIndex]; break;
                case Animal.Cow: texture = cowTextures[cowIndex]; break;
                case Animal.Duck: texture = duckTextures[0]; break;
                case Animal.Rabbit: texture = rabbitTextures[0]; break;
                case Animal.Goat: texture = goatTextures[0]; break;
                case Animal.Pig: texture = pigTextures[0]; break;
                case Animal.Sheep: texture = sheepTextures[0]; break;
            }

            Game1.spriteBatch.Draw(texture, new Rectangle(mx - dx, my - 64 - dy, w, h), Color.White);
            
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
                choosingAnimal = false;
                drawAnimal = false;
            }

            PurchaseAnimalsMenu menu = Game1.activeClickableMenu as PurchaseAnimalsMenu;

            if (menu != null) {

                FarmAnimal animal = Helper.Reflection.GetField<FarmAnimal>(Game1.activeClickableMenu, "animalBeingPurchased").GetValue();
                if (e.Button == SButton.MouseLeft) {

                    if (menu.doneNamingButton.containsPoint((int)e.Cursor.ScreenPixels.X, (int)e.Cursor.ScreenPixels.Y) ||
                        menu.okButton.containsPoint((int)e.Cursor.ScreenPixels.X, (int)e.Cursor.ScreenPixels.Y)) {
                        choosingAnimal = false;
                        drawAnimal = false;
                    }

                    Building buildingAt = Game1.getFarm().getBuildingAt(new Vector2(e.Cursor.AbsolutePixels.X, e.Cursor.AbsolutePixels.Y) / 64);
                    if (buildingAt != null && buildingAt.buildingType.Value.Contains(animal.buildingTypeILiveIn.Value) && 
                        !(buildingAt.indoors.Value as AnimalHouse).isFull()) {
                        choosingAnimal = false;
                        drawAnimal = false;
                    }
                    
                    foreach (ClickableTextureComponent component in menu.animalsToPurchase) {
                        if (component.containsPoint((int)e.Cursor.ScreenPixels.X, (int)e.Cursor.ScreenPixels.Y)) {
                            if (Game1.player.money >= component.item.salePrice()) {
                                string type = component.item.Name;
                                if (type != null) {
                                    choosingAnimal = true;
                                    if (type.Contains("Chicken")) {
                                        currentAnimal = Animal.Chicken;
                                    } else if (type.Contains("Cow")) {
                                        currentAnimal = Animal.Cow;
                                    } else if (type.Contains("Pig")) {
                                        currentAnimal = Animal.Pig;
                                    } else if (type.Contains("Goat")) {
                                        currentAnimal = Animal.Goat;
                                    } else if (type.Contains("Sheep")) {
                                        currentAnimal = Animal.Sheep;
                                    } else if (type.Contains("Rabbit")) {
                                        currentAnimal = Animal.Rabbit;
                                    } else if (type.Contains("Duck")) {
                                        currentAnimal = Animal.Duck;
                                    } else {
                                        currentAnimal = Animal.None;
                                        choosingAnimal = false;
                                    }
                                    break;
                                }
                            }
                        }
                    }
                }

                if (currentAnimal == Animal.None) {
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
                    switch (currentAnimal) {
                        case Animal.Chicken:
                            chickenIndex = (delta + chickenIndex + chickens.Count) % chickens.Count;
                            while (!IsChickenTypeUnlocked(chickenIndex)) {
                                chickenIndex = (delta + chickenIndex + chickens.Count) % chickens.Count;
                            }
                            break;
                        case Animal.Cow:
                            cowIndex = (delta + cowIndex + cows.Count) % cows.Count;
                            break;
                    }

                    if (currentAnimal == Animal.Chicken || currentAnimal == Animal.Cow) {
                        animal.displayType = (currentAnimal == Animal.Chicken) ? chickens[chickenIndex] : cows[cowIndex];
                    }
                }
            }
        }

        private bool IsChickenTypeUnlocked(int type) {
            switch (type) {
                case 0: return true;
                case 1: return true;
                case 2:
                    int eggsShipped, mayoShipped;
                    Game1.player.basicShipped.TryGetValue(305, out eggsShipped);
                    Game1.player.basicShipped.TryGetValue(308, out mayoShipped);
                    return Game1.player.eventsSeen.Contains(942069) || Game1.player.hasRustyKey || Config.EnableVoidChickens || eggsShipped > 0 || mayoShipped > 0;
                case 3: return Game1.player.eventsSeen.Contains(3900074) || Config.EnableBlueChickens;
                default: return false;
            }
        }

        /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnMenuChanged(object sender, MenuChangedEventArgs e) {

            PurchaseAnimalsMenu menu2 = e.OldMenu as PurchaseAnimalsMenu;
            if (menu2 != null) {
                FarmAnimal animal = Helper.Reflection.GetField<FarmAnimal>(menu2, "animalBeingPurchased").GetValue();
                if (animal != null) {

                    string data = null;
                    string key = "";

                    if (animal.type.Value.Contains("Chicken")) {
                        key = chickens[chickenIndex];
                    } else if (animal.type.Value.Contains("Cow")) {
                        key = cows[cowIndex];
                    } else if (animal.type.Value.Contains("Duck")) {
                        key = "Duck";
                    } else if (animal.type.Value.Contains("Goat")) {
                        key = "Goat";
                    } else if (animal.type.Value.Contains("Pig")) {
                        key = "Pig";
                    } else if (animal.type.Value.Contains("Rabbit")) {
                        key = "Rabbit";
                    } else if (animal.type.Value.Contains("Sheep")) {
                        key = "Sheep";
                    } else {
                        Monitor.Log($"Invalid animal type: {animal.type.Value}", LogLevel.Warn);
                        return;
                    }

                    animalData.TryGetValue(key, out data);

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
            choosingAnimal = false;
        }
    }
}
