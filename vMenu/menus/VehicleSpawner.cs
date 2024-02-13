using System.Collections.Generic;
using System.Linq;

using CitizenFX.Core;

using MenuAPI;

using vMenuClient.data;

using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuShared.PermissionsManager;

namespace vMenuClient.menus
{
    public class VehicleSpawner
    {
        // Variables
        private Menu menu;
        public static Dictionary<string, uint> AddonVehicles;

        public bool SpawnInVehicle { get; private set; } = UserDefaults.VehicleSpawnerSpawnInside;
        public bool ReplaceVehicle { get; private set; } = UserDefaults.VehicleSpawnerReplacePrevious;
        public static List<bool> allowedCategories;

        private void CreateMenu()
        {
            #region initial setup.
            // Create the menu.
            menu = new Menu(Game.Player.Name, "Spawn de véhicule");

            // Create the buttons and checkboxes.
            var spawnByName = new MenuItem("Spawn un véhicule via son nom", "Entrer le nom d'un véhicule à faire spawn.");
            var spawnInVeh = new MenuCheckboxItem("Apparaître à l'intérieur du véhicule", "Cela fonctionne uniquement pour les véhicules non terrestre.", SpawnInVehicle);
            var replacePrev = new MenuCheckboxItem("Remplacer le dernier véhicule", "Cela fonctionne uniquement pour les véhicules non terrestre.", ReplaceVehicle);

            // Add the items to the menu.
            if (IsAllowed(Permission.VSSpawnByName))
            {
                menu.AddMenuItem(spawnByName);
            }
            menu.AddMenuItem(spawnInVeh);
            menu.AddMenuItem(replacePrev);
            #endregion

            #region addon cars menu
            // Vehicle Addons List
            var addonCarsMenu = new Menu("Véhicules moddés", "Faire apparaître un véhicule moddé");
            var addonCarsBtn = new MenuItem("Véhicules moddés", "Une liste de tous les véhicules moddés présents sur le serveur.") { Label = "→→→" };

            menu.AddMenuItem(addonCarsBtn);

            if (IsAllowed(Permission.VSAddon))
            {
                if (AddonVehicles != null)
                {
                    if (AddonVehicles.Count > 0)
                    {
                        MenuController.BindMenuItem(menu, addonCarsMenu, addonCarsBtn);
                        MenuController.AddSubmenu(menu, addonCarsMenu);
                        var unavailableCars = new Menu("Spawner - Véhicules moddés", "Véhicules indisponibles");
                        var unavailableCarsBtn = new MenuItem("Unavailable Vehicles", "These addon vehicles are not currently being streamed (correctly) and are not able to be spawned.") { Label = "→→→" };
                        MenuController.AddSubmenu(addonCarsMenu, unavailableCars);

                        for (var cat = 0; cat < 23; cat++)
                        {
                            var categoryMenu = new Menu("Spawner - Véhicules moddés", GetLabelText($"VEH_CLASS_{cat}"));
                            var categoryBtn = new MenuItem(GetLabelText($"VEH_CLASS_{cat}"), $"Faire apparaître un véhicule de classe : {GetLabelText($"VEH_CLASS_{cat}")}.") { Label = "→→→" };

                            addonCarsMenu.AddMenuItem(categoryBtn);

                            if (!allowedCategories[cat])
                            {
                                categoryBtn.Description = "Cette classe de véhicule est désactivé sur le serveur.";
                                categoryBtn.Enabled = false;
                                categoryBtn.LeftIcon = MenuItem.Icon.LOCK;
                                categoryBtn.Label = "";
                                continue;
                            }

                            // Loop through all addon vehicles in this class.
                            foreach (var veh in AddonVehicles.Where(v => GetVehicleClassFromName(v.Value) == cat))
                            {
                                var localizedName = GetLabelText(GetDisplayNameFromVehicleModel(veh.Value));

                                var name = localizedName != "NULL" ? localizedName : GetDisplayNameFromVehicleModel(veh.Value);
                                name = name != "CARNOTFOUND" ? name : veh.Key;

                                var carBtn = new MenuItem(name, $"Cliquer pour faire apparaître {name}.")
                                {
                                    Label = $"({veh.Key})",
                                    ItemData = veh.Key // store the model name in the button data.
                                };

                                // This should be impossible to be false, but we check it anyway.
                                if (IsModelInCdimage(veh.Value))
                                {
                                    categoryMenu.AddMenuItem(carBtn);
                                }
                                else
                                {
                                    carBtn.Enabled = false;
                                    carBtn.Description = "Ce véhicule n'est pas disponible. Merci de contacter l'équipe de modération.";
                                    carBtn.LeftIcon = MenuItem.Icon.LOCK;
                                    unavailableCars.AddMenuItem(carBtn);
                                }
                            }

                            //if (AddonVehicles.Count(av => GetVehicleClassFromName(av.Value) == cat && IsModelInCdimage(av.Value)) > 0)
                            if (categoryMenu.Size > 0)
                            {
                                MenuController.AddSubmenu(addonCarsMenu, categoryMenu);
                                MenuController.BindMenuItem(addonCarsMenu, categoryMenu, categoryBtn);

                                categoryMenu.OnItemSelect += (sender, item, index) =>
                                {
                                    SpawnVehicle(item.ItemData.ToString(), SpawnInVehicle, ReplaceVehicle);
                                };
                            }
                            else
                            {
                                categoryBtn.Description = "Il n'y a aucun véhicule moddé disponible dans cette catégorie.";
                                categoryBtn.Enabled = false;
                                categoryBtn.LeftIcon = MenuItem.Icon.LOCK;
                                categoryBtn.Label = "";
                            }
                        }

                        if (unavailableCars.Size > 0)
                        {
                            addonCarsMenu.AddMenuItem(unavailableCarsBtn);
                            MenuController.BindMenuItem(addonCarsMenu, unavailableCars, unavailableCarsBtn);
                        }
                    }
                    else
                    {
                        addonCarsBtn.Enabled = false;
                        addonCarsBtn.LeftIcon = MenuItem.Icon.LOCK;
                        addonCarsBtn.Description = "Il n'y a aucun véhicule moddé disponible sur le serveur.";
                    }
                }
                else
                {
                    addonCarsBtn.Enabled = false;
                    addonCarsBtn.LeftIcon = MenuItem.Icon.LOCK;
                    addonCarsBtn.Description = "La liste des véhicules moddés ne peut être lues.";
                }
            }
            else
            {
                addonCarsBtn.Enabled = false;
                addonCarsBtn.LeftIcon = MenuItem.Icon.LOCK;
                addonCarsBtn.Description = "L'accès à la liste des véhicules moddés est interdit sur le serveur.";
            }
            #endregion

            // These are the max speed, acceleration, braking and traction values per vehicle class.
            var speedValues = new float[23]
            {
                44.9374657f,
                50.0000038f,
                48.862133f,
                48.1321335f,
                50.7077942f,
                51.3333359f,
                52.3922348f,
                53.86687f,
                52.03867f,
                49.2241631f,
                39.6176529f,
                37.5559425f,
                42.72843f,
                21.0f,
                45.0f,
                65.1952744f,
                109.764259f,
                42.72843f,
                56.5962219f,
                57.5398865f,
                43.3140678f,
                26.66667f,
                53.0537224f
            };
            var accelerationValues = new float[23]
            {
                0.34f,
                0.29f,
                0.335f,
                0.28f,
                0.395f,
                0.39f,
                0.66f,
                0.42f,
                0.425f,
                0.475f,
                0.21f,
                0.3f,
                0.32f,
                0.17f,
                18.0f,
                5.88f,
                21.0700016f,
                0.33f,
                14.0f,
                6.86f,
                0.32f,
                0.2f,
                0.76f
            };
            var brakingValues = new float[23]
            {
                0.72f,
                0.95f,
                0.85f,
                0.9f,
                1.0f,
                1.0f,
                1.3f,
                1.25f,
                1.52f,
                1.1f,
                0.6f,
                0.7f,
                0.8f,
                3.0f,
                0.4f,
                3.5920403f,
                20.58f,
                0.9f,
                2.93960738f,
                3.9472363f,
                0.85f,
                5.0f,
                1.3f
            };
            var tractionValues = new float[23]
            {
                2.3f,
                2.55f,
                2.3f,
                2.6f,
                2.625f,
                2.65f,
                2.8f,
                2.782f,
                2.9f,
                2.95f,
                2.0f,
                3.3f,
                2.175f,
                2.05f,
                0.0f,
                1.6f,
                2.15f,
                2.55f,
                2.57f,
                3.7f,
                2.05f,
                2.5f,
                3.2925f
            };

            #region vehicle classes submenus
            // Loop through all the vehicle classes.
            for (var vehClass = 0; vehClass < 23; vehClass++)
            {
                // Get the class name.
                var className = GetLabelText($"VEH_CLASS_{vehClass}");

                // Create a button & a menu for it, add the menu to the menu pool and add & bind the button to the menu.
                var btn = new MenuItem(className, $"Faire apparaître un véhicule de classe ~o~{className} ~s~.")
                {
                    Label = "→→→"
                };

                var vehicleClassMenu = new Menu("Véhicule Spawner", className);

                MenuController.AddSubmenu(menu, vehicleClassMenu);
                menu.AddMenuItem(btn);

                if (allowedCategories[vehClass])
                {
                    MenuController.BindMenuItem(menu, vehicleClassMenu, btn);
                }
                else
                {
                    btn.LeftIcon = MenuItem.Icon.LOCK;
                    btn.Description = "Cette catégorie est désactivée sur le serveur.";
                    btn.Enabled = false;
                }

                // Create a dictionary for the duplicate vehicle names (in this vehicle class).
                var duplicateVehNames = new Dictionary<string, int>();

                #region Add vehicles per class
                // Loop through all the vehicles in the vehicle class.
                foreach (var veh in VehicleData.Vehicles.VehicleClasses[className])
                {
                    // Convert the model name to start with a Capital letter, converting the other characters to lowercase. 
                    var properCasedModelName = veh[0].ToString().ToUpper() + veh.ToLower().Substring(1);

                    // Get the localized vehicle name, if it's "NULL" (no label found) then use the "properCasedModelName" created above.
                    var vehName = GetVehDisplayNameFromModel(veh) != "NULL" ? GetVehDisplayNameFromModel(veh) : properCasedModelName;
                    var vehModelName = veh;
                    var model = (uint)GetHashKey(vehModelName);

                    var topSpeed = Map(GetVehicleModelEstimatedMaxSpeed(model), 0f, speedValues[vehClass], 0f, 1f);
                    var acceleration = Map(GetVehicleModelAcceleration(model), 0f, accelerationValues[vehClass], 0f, 1f);
                    var maxBraking = Map(GetVehicleModelMaxBraking(model), 0f, brakingValues[vehClass], 0f, 1f);
                    var maxTraction = Map(GetVehicleModelMaxTraction(model), 0f, tractionValues[vehClass], 0f, 1f);

                    // Loop through all the menu items and check each item's title/text and see if it matches the current vehicle (display) name.
                    var duplicate = false;
                    for (var itemIndex = 0; itemIndex < vehicleClassMenu.Size; itemIndex++)
                    {
                        // If it matches...
                        if (vehicleClassMenu.GetMenuItems()[itemIndex].Text.ToString() == vehName)
                        {

                            // Check if the model was marked as duplicate before.
                            if (duplicateVehNames.Keys.Contains(vehName))
                            {
                                // If so, add 1 to the duplicate counter for this model name.
                                duplicateVehNames[vehName]++;
                            }

                            // If this is the first duplicate, then set it to 2.
                            else
                            {
                                duplicateVehNames[vehName] = 2;
                            }

                            // The model name is a duplicate, so get the modelname and add the duplicate amount for this model name to the end of the vehicle name.
                            vehName += $" ({duplicateVehNames[vehName]})";

                            // Then create and add a new button for this vehicle.

                            if (DoesModelExist(veh))
                            {
                                var vehBtn = new MenuItem(vehName)
                                {
                                    Enabled = true,
                                    Label = $"({vehModelName.ToLower()})",
                                    ItemData = new float[4] { topSpeed, acceleration, maxBraking, maxTraction }
                                };
                                vehicleClassMenu.AddMenuItem(vehBtn);
                            }
                            else
                            {
                                var vehBtn = new MenuItem(vehName, "Ce véhicule n'est pas disponible en raison d'un problème technique. Merci de contacter l'équipe du serveur.")
                                {
                                    Enabled = false,
                                    Label = $"({vehModelName.ToLower()})",
                                    ItemData = new float[4] { 0f, 0f, 0f, 0f }
                                };
                                vehicleClassMenu.AddMenuItem(vehBtn);
                                vehBtn.RightIcon = MenuItem.Icon.LOCK;
                            }

                            // Mark duplicate as true and break from the loop because we already found the duplicate.
                            duplicate = true;
                            break;
                        }
                    }

                    // If it's not a duplicate, add the model name.
                    if (!duplicate)
                    {
                        if (DoesModelExist(veh))
                        {
                            var vehBtn = new MenuItem(vehName)
                            {
                                Enabled = true,
                                Label = $"({vehModelName.ToLower()})",
                                ItemData = new float[4] { topSpeed, acceleration, maxBraking, maxTraction }
                            };
                            vehicleClassMenu.AddMenuItem(vehBtn);
                        }
                        else
                        {
                            var vehBtn = new MenuItem(vehName, "Ce véhicule n'est pas disponible en raison d'un problème technique. Merci de contacter l'équipe du serveur.")
                            {
                                Enabled = false,
                                Label = $"({vehModelName.ToLower()})",
                                ItemData = new float[4] { 0f, 0f, 0f, 0f }
                            };
                            vehicleClassMenu.AddMenuItem(vehBtn);
                            vehBtn.RightIcon = MenuItem.Icon.LOCK;
                        }
                    }
                }
                #endregion

                vehicleClassMenu.ShowVehicleStatsPanel = true;

                // Handle button presses
                vehicleClassMenu.OnItemSelect += async (sender2, item2, index2) =>
                {
                    await SpawnVehicle(VehicleData.Vehicles.VehicleClasses[className][index2], SpawnInVehicle, ReplaceVehicle);
                };

                static void HandleStatsPanel(Menu openedMenu, MenuItem currentItem)
                {
                    if (currentItem != null)
                    {
                        if (currentItem.ItemData is float[] data)
                        {
                            openedMenu.ShowVehicleStatsPanel = true;
                            openedMenu.SetVehicleStats(data[0], data[1], data[2], data[3]);
                            openedMenu.SetVehicleUpgradeStats(0f, 0f, 0f, 0f);
                        }
                        else
                        {
                            openedMenu.ShowVehicleStatsPanel = false;
                        }
                    }
                }

                vehicleClassMenu.OnMenuOpen += (m) =>
                {
                    HandleStatsPanel(m, m.GetCurrentMenuItem());
                };

                vehicleClassMenu.OnIndexChange += (m, oldItem, newItem, oldIndex, newIndex) =>
                {
                    HandleStatsPanel(m, newItem);
                };
            }
            #endregion

            #region handle events
            // Handle button presses.
            menu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == spawnByName)
                {
                    // Passing "custom" as the vehicle name, will ask the user for input.
                    await SpawnVehicle("custom", SpawnInVehicle, ReplaceVehicle);
                }
            };

            // Handle checkbox changes.
            menu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                if (item == spawnInVeh)
                {
                    SpawnInVehicle = _checked;
                }
                else if (item == replacePrev)
                {
                    ReplaceVehicle = _checked;
                }
            };
            #endregion
        }

        /// <summary>
        /// Create the menu if it doesn't exist, and then returns it.
        /// </summary>
        /// <returns>The Menu</returns>
        public Menu GetMenu()
        {
            if (menu == null)
            {
                CreateMenu();
            }
            return menu;
        }
    }
}
