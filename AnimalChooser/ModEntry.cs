using AnimalChooser.Integrations;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Paritee.StardewValleyAPI.Buildings.AnimalShop.FarmAnimals;
using PariteeFarmAnimal = Paritee.StardewValleyAPI.FarmAnimals.FarmAnimal;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using Paritee.StardewValleyAPI.Players;

namespace AnimalChooser
{
    public class ModEntry : Mod {
        private ModConfig Config;
        private Integrations.PariteeIntegration PariteeIntegration;

        private bool choosingAnimal = false;
        private bool drawAnimal = false;
        private string currentStockSelection;
        private KeyValuePair<int, Paritee.StardewValleyAPI.FarmAnimals.FarmAnimal> currentAnimal;
        private int heartLevel = 0;

        private Texture2D heartFullTexture;
        private Texture2D heartEmptyTexture;

        public override void Entry(IModHelper helper)
        {
            this.Config = helper.ReadConfig<ModConfig>();
            this.heartFullTexture = helper.Content.Load<Texture2D>("Assets/heartFull.png");
            this.heartEmptyTexture = helper.Content.Load<Texture2D>("Assets/heartEmpty.png");

            // Integration events
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;

            // Events
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Input.MouseWheelScrolled += OnMouseWheelScrolled;
            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.Display.Rendered += OnRendered;
            helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdateTicked;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // Integrate with Paritee and BFAV
            this.PariteeIntegration = new PariteeIntegration(this.Helper);
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            this.PariteeIntegration.Setup(new Player(Game1.player, this.Helper));
            this.SanitizeFarmAnimalsForPurchase();
        }

        private void SanitizeFarmAnimalsForPurchase()
        {
            FarmAnimalForPurchase chicken = this.PariteeIntegration.GetFarmAnimalForPurchase(PariteeIntegration.VanillaChicken);

            // Check for just in case someone removed chickens from their game
            if (chicken == null)
            {
                return;
            }
            
            List<string> sanitized = new List<string>();

            foreach (string type in chicken.FarmAnimalTypes)
            {
                PariteeIntegration.Chickens variation = this.PariteeIntegration.DetermineChickenVariation(type);

                if (this.IsChickenTypeUnlocked(variation))
                {
                    sanitized.Add(type);
                }
            }

            chicken.FarmAnimalTypes = sanitized;
        }

        /// <summary>Raised after the player scrolls the mouse wheel.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnMouseWheelScrolled(object sender, MouseWheelScrolledEventArgs e)
        {
            if (!this.drawAnimal)
            {
                return;
            }

            if (e.Delta < 0 && this.heartLevel > 0)
            {
                this.heartLevel -= 1;
            }
            else if (e.Delta > 0 && this.heartLevel < 5)
            {
                this.heartLevel += 1;
            }
            else
            {
                return;
            }

            Game1.playSound("smallSelect");
        }

        /// <summary>Raised once per second after the game state is updated.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnOneSecondUpdateTicked(object sender, OneSecondUpdateTickedEventArgs e)
        {
            if (!this.choosingAnimal)
            {
                return;
            }

            if (Game1.activeClickableMenu is PurchaseAnimalsMenu menu)
            {
                if (Helper.Reflection.GetField<StardewValley.FarmAnimal>(menu, "animalBeingPurchased").GetValue() != null)
                {
                    this.Helper.Reflection.GetField<StardewValley.FarmAnimal>(menu, "animalBeingPurchased").SetValue(this.currentAnimal.Value.ToFarmAnimal());
                }
            }

            this.drawAnimal = true;
        }

        /// <summary>Raised after the game draws to the sprite patch in a draw tick, just before the final sprite batch is rendered to the screen.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnRendered(object sender, RenderedEventArgs e)
        {
            if (!this.choosingAnimal)
            {
                return;
            }

            if (Game1.globalFade)
            {
                return;
            }
            
            int dy, dx, w, h;

            if (this.PariteeIntegration.IsCoopDweller(this.currentAnimal.Value))
            {
                dx = 24;
                dy = 24;
                w = 64;
                h = 64;
            }
            else
            {
                dx = 64;
                dy = 88;
                w = 128;
                h = 128;
            }

            dy += Convert.ToInt32(16 * Math.Sin(0.05 * Game1.ticks));
            dy += this.heartLevel > 0 ? 24 : 0;

            int mx = Game1.getMouseX();
            int my = Game1.getMouseY();

            this.PariteeIntegration.DrawFarmAnimal(this.currentAnimal.Value, mx - dx, my - 64 - dy, w, h);

            if (this.heartLevel > 0)
            {
                for (int i = 0; i < 5; i++)
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

            if (Game1.activeClickableMenu is PurchaseAnimalsMenu menu)
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
                    else if (this.Helper.Reflection.GetField<bool>(menu, "namingAnimal").GetValue())
                    {
                        this.choosingAnimal = false;
                        this.drawAnimal = false;
                    }
                    else
                    {
                        foreach (ClickableTextureComponent component in menu.animalsToPurchase)
                        {
                            if (component.containsPoint((int)e.Cursor.ScreenPixels.X, (int)e.Cursor.ScreenPixels.Y))
                            {
                                string stockNameString = component.item.Name;

                                if (stockNameString != null)
                                {
                                    this.choosingAnimal = true;
                                    this.currentStockSelection = stockNameString;

                                    int index = 0;
                                    string type = this.PariteeIntegration.GetTypeAtIndex(this.currentStockSelection, index);
                                    PariteeFarmAnimal animal = this.PariteeIntegration.GetFarmAnimal(type);
                                    this.currentAnimal = new KeyValuePair<int, PariteeFarmAnimal>(index, animal);

                                    break;
                                }
                            }
                        }
                    }
                }

                if (!this.choosingAnimal)
                {
                    return;
                }

                int delta = 0;

                if (e.Button == SButton.Left)
                {
                    delta = -1;
                }
                else if (e.Button == SButton.Right)
                {
                    delta = 1;
                }

                if (delta != 0)
                {
                    int total = this.PariteeIntegration.GetFarmAnimalForPurchase(currentStockSelection).FarmAnimalTypes.Count;
                    int index = (delta + this.currentAnimal.Key + total) % total;
                    string type = this.PariteeIntegration.GetTypeAtIndex(this.currentStockSelection, index);
                    PariteeFarmAnimal animal = this.PariteeIntegration.GetFarmAnimal(type, this.currentAnimal.Value.myID.Value, this.currentAnimal.Value.ownerID.Value);
                    this.currentAnimal = new KeyValuePair<int, PariteeFarmAnimal>(index, animal);
                }
            }
        }

        private bool IsChickenTypeUnlocked(PariteeIntegration.Chickens type)
        {
            switch (type)
            {
                case PariteeIntegration.Chickens.Void:
                    {
                        Game1.player.basicShipped.TryGetValue(305, out int eggsShipped);
                        Game1.player.basicShipped.TryGetValue(308, out int mayoShipped);

                        return Game1.player.eventsSeen.Contains(942069) || Game1.player.hasRustyKey || this.Config.EnableVoidChickens || eggsShipped > 0 || mayoShipped > 0;
                    }

                case PariteeIntegration.Chickens.Blue:
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
            if (e.OldMenu is PurchaseAnimalsMenu menu2)
            {
                StardewValley.FarmAnimal animalBeingPurchased = this.Helper.Reflection.GetField<StardewValley.FarmAnimal>(menu2, "animalBeingPurchased").GetValue();

                if (animalBeingPurchased != null)
                {
                    this.currentAnimal.Value.SetFriendshipHearts(this.heartLevel);

                    if (Config.AnimalStartsAsAdult)
                    {
                        this.currentAnimal.Value.BecomeAnAdult();
                    }

                    this.Helper.Reflection.GetField<StardewValley.FarmAnimal>(menu2, "animalBeingPurchased").SetValue(this.currentAnimal.Value.ToFarmAnimal());
                }
            }

            this.choosingAnimal = false;
            this.heartLevel = 0;
        }
    }
}
