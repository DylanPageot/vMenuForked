using System.Collections.Generic;

using CitizenFX.Core;

using MenuAPI;

using Newtonsoft.Json;

using vMenuClient.data;

using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuShared.PermissionsManager;

namespace vMenuClient.menus
{
    public class WeaponLoadouts
    {
        // Variables
        private Menu menu = null;
        private readonly Menu SavedLoadoutsMenu = new("Saved Loadouts", "saved weapon loadouts list");
        private readonly Menu ManageLoadoutMenu = new("Manage Loadout", "Manage saved weapon loadout");
        public bool WeaponLoadoutsSetLoadoutOnRespawn { get; private set; } = UserDefaults.WeaponLoadoutsSetLoadoutOnRespawn;

        private readonly Dictionary<string, List<ValidWeapon>> SavedWeapons = new();

        public static Dictionary<string, List<ValidWeapon>> GetSavedWeapons()
        {
            var handle = StartFindKvp("vmenu_string_saved_weapon_loadout_");
            var saves = new Dictionary<string, List<ValidWeapon>>();
            while (true)
            {
                var kvp = FindKvp(handle);
                if (string.IsNullOrEmpty(kvp))
                {
                    break;
                }
                saves.Add(kvp, JsonConvert.DeserializeObject<List<ValidWeapon>>(GetResourceKvpString(kvp)));
            }
            EndFindKvp(handle);
            return saves;
        }

        private string SelectedSavedLoadoutName { get; set; } = "";
        // vmenu_temp_weapons_loadout_before_respawn
        // vmenu_string_saved_weapon_loadout_

        /// <summary>
        /// Returns the saved weapons list, as well as sets the <see cref="SavedWeapons"/> variable.
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, List<ValidWeapon>> RefreshSavedWeaponsList()
        {
            if (SavedWeapons.Count > 0)
            {
                SavedWeapons.Clear();
            }

            var handle = StartFindKvp("vmenu_string_saved_weapon_loadout_");
            var saves = new List<string>();
            while (true)
            {
                var kvp = FindKvp(handle);
                if (string.IsNullOrEmpty(kvp))
                {
                    break;
                }
                saves.Add(kvp);
            }
            EndFindKvp(handle);

            foreach (var save in saves)
            {
                SavedWeapons.Add(save, JsonConvert.DeserializeObject<List<ValidWeapon>>(GetResourceKvpString(save)));
            }

            return SavedWeapons;
        }

        /// <summary>
        /// Creates the menu if it doesn't exist yet and sets the event handlers.
        /// </summary>
        public void CreateMenu()
        {
            menu = new Menu(Game.Player.Name, "Gestion des loadouts sauvegardés");

            MenuController.AddSubmenu(menu, SavedLoadoutsMenu);
            MenuController.AddSubmenu(SavedLoadoutsMenu, ManageLoadoutMenu);

            var saveLoadout = new MenuItem("Sauvegarder loadout", "Sauvegarder vos armes dans un nouveau loadout vierge.");
            var savedLoadoutsMenuBtn = new MenuItem("Gérer vos loadouts", "Gérer ses loadouts sauvegardés.") { Label = "→→→" };
            var enableDefaultLoadouts = new MenuCheckboxItem("Restaurer le loadout par défaut", "Si vous avez indiqué un loadout comme étant celui par défaut, vous respawnerez automatiquement avec ce dernier.", WeaponLoadoutsSetLoadoutOnRespawn);

            menu.AddMenuItem(saveLoadout);
            menu.AddMenuItem(savedLoadoutsMenuBtn);
            MenuController.BindMenuItem(menu, SavedLoadoutsMenu, savedLoadoutsMenuBtn);
            if (IsAllowed(Permission.WLEquipOnRespawn))
            {
                menu.AddMenuItem(enableDefaultLoadouts);

                menu.OnCheckboxChange += (sender, checkbox, index, _checked) =>
                {
                    WeaponLoadoutsSetLoadoutOnRespawn = _checked;
                };
            }


            void RefreshSavedWeaponsMenu()
            {
                var oldCount = SavedLoadoutsMenu.Size;
                SavedLoadoutsMenu.ClearMenuItems(true);

                RefreshSavedWeaponsList();

                foreach (var sw in SavedWeapons)
                {
                    var btn = new MenuItem(sw.Key.Replace("vmenu_string_saved_weapon_loadout_", ""), "Click to manage this loadout.") { Label = "→→→" };
                    SavedLoadoutsMenu.AddMenuItem(btn);
                    MenuController.BindMenuItem(SavedLoadoutsMenu, ManageLoadoutMenu, btn);
                }

                if (oldCount > SavedWeapons.Count)
                {
                    SavedLoadoutsMenu.RefreshIndex();
                }
            }


            var spawnLoadout = new MenuItem("Equiper loadout", "Cela vous permettra de vous équiper de ce loadout et supprimera vos armes actuelles.");
            var renameLoadout = new MenuItem("Renommer loadout", "Renomme ce loadout déjà enregistré");
            var cloneLoadout = new MenuItem("Dupliquer loadout", "Duplique ce loadout vers un nouveau slot vide.");
            var setDefaultLoadout = new MenuItem("Définir par défaut", "Définis ce loadout comme étant celui par défaut.");
            var replaceLoadout = new MenuItem("~r~Remplacer loadout", "~r~Cela remplacer le loadout par les armes que vous possédez actuellement. Ne peux être annulé !");
            var deleteLoadout = new MenuItem("~r~Supprimer loadout", "~r~Cela supprimera totalement le loadout. Ne peux être annulé !");

            if (IsAllowed(Permission.WLEquip))
            {
                ManageLoadoutMenu.AddMenuItem(spawnLoadout);
            }

            ManageLoadoutMenu.AddMenuItem(renameLoadout);
            ManageLoadoutMenu.AddMenuItem(cloneLoadout);
            ManageLoadoutMenu.AddMenuItem(setDefaultLoadout);
            ManageLoadoutMenu.AddMenuItem(replaceLoadout);
            ManageLoadoutMenu.AddMenuItem(deleteLoadout);

            // Save the weapons loadout.
            menu.OnItemSelect += async (sender, item, index) =>
            {
                if (item == saveLoadout)
                {
                    var name = await GetUserInput("Enter a save name", 30);
                    if (string.IsNullOrEmpty(name))
                    {
                        Notify.Error(CommonErrors.InvalidInput);
                    }
                    else
                    {
                        if (SavedWeapons.ContainsKey("vmenu_string_saved_weapon_loadout_" + name))
                        {
                            Notify.Error(CommonErrors.SaveNameAlreadyExists);
                        }
                        else
                        {
                            if (SaveWeaponLoadout("vmenu_string_saved_weapon_loadout_" + name))
                            {
                                Log("saveweapons called from menu select (save loadout button)");
                                Notify.Success($"Votre loadout a bien été sauvegardé comme : ~g~<C>{name}</C>~s~.");
                            }
                            else
                            {
                                Notify.Error(CommonErrors.UnknownError);
                            }
                        }
                    }
                }
            };

            // manage spawning, renaming, deleting etc.
            ManageLoadoutMenu.OnItemSelect += async (sender, item, index) =>
            {
                if (SavedWeapons.ContainsKey(SelectedSavedLoadoutName))
                {
                    var weapons = SavedWeapons[SelectedSavedLoadoutName];

                    if (item == spawnLoadout) // spawn
                    {
                        await SpawnWeaponLoadoutAsync(SelectedSavedLoadoutName, false, true, false);
                    }
                    else if (item == renameLoadout || item == cloneLoadout) // rename or clone
                    {
                        var newName = await GetUserInput("Entrer un nom", SelectedSavedLoadoutName.Replace("vmenu_string_saved_weapon_loadout_", ""), 30);
                        if (string.IsNullOrEmpty(newName))
                        {
                            Notify.Error(CommonErrors.InvalidInput);
                        }
                        else
                        {
                            if (SavedWeapons.ContainsKey("vmenu_string_saved_weapon_loadout_" + newName))
                            {
                                Notify.Error(CommonErrors.SaveNameAlreadyExists);
                            }
                            else
                            {
                                SetResourceKvp("vmenu_string_saved_weapon_loadout_" + newName, JsonConvert.SerializeObject(weapons));
                                Notify.Success($"Votre loadout a bien été {(item == renameLoadout ? "renommé" : "supprimé")} en  ~g~<C>{newName}</C>~s~.");

                                if (item == renameLoadout)
                                {
                                    DeleteResourceKvp(SelectedSavedLoadoutName);
                                }

                                ManageLoadoutMenu.GoBack();
                            }
                        }
                    }
                    else if (item == setDefaultLoadout) // set as default
                    {
                        SetResourceKvp("vmenu_string_default_loadout", SelectedSavedLoadoutName);
                        Notify.Success("Cela sera votre nouveau loadout par défaut.");
                        item.LeftIcon = MenuItem.Icon.TICK;
                    }
                    else if (item == replaceLoadout) // replace
                    {
                        if (replaceLoadout.Label == "Êtes vous sûrs ?")
                        {
                            replaceLoadout.Label = "";
                            SaveWeaponLoadout(SelectedSavedLoadoutName);
                            Log("save weapons called from replace loadout");
                            Notify.Success("Votre loadout a bien été remplacé par les armes que vous possédez actuellement.");
                        }
                        else
                        {
                            replaceLoadout.Label = "Êtes vous sûrs ?";
                        }
                    }
                    else if (item == deleteLoadout) // delete
                    {
                        if (deleteLoadout.Label == "Êtes vous sûrs ?")
                        {
                            deleteLoadout.Label = "";
                            DeleteResourceKvp(SelectedSavedLoadoutName);
                            ManageLoadoutMenu.GoBack();
                            Notify.Success("Votre ladout a bien été supprimé.");
                        }
                        else
                        {
                            deleteLoadout.Label = "Êtes vous sûrs ?";
                        }
                    }
                }
            };

            // Reset the 'are you sure' states.
            ManageLoadoutMenu.OnMenuClose += (sender) =>
            {
                deleteLoadout.Label = "";
                renameLoadout.Label = "";
            };
            // Reset the 'are you sure' states.
            ManageLoadoutMenu.OnIndexChange += (sender, oldItem, newItem, oldIndex, newIndex) =>
            {
                deleteLoadout.Label = "";
                renameLoadout.Label = "";
            };

            // Refresh the spawned weapons menu whenever this menu is opened.
            SavedLoadoutsMenu.OnMenuOpen += (sender) =>
            {
                RefreshSavedWeaponsMenu();
            };

            // Set the current saved loadout whenever a loadout is selected.
            SavedLoadoutsMenu.OnItemSelect += (sender, item, index) =>
            {
                if (SavedWeapons.ContainsKey("vmenu_string_saved_weapon_loadout_" + item.Text))
                {
                    SelectedSavedLoadoutName = "vmenu_string_saved_weapon_loadout_" + item.Text;
                }
                else // shouldn't ever happen, but just in case
                {
                    ManageLoadoutMenu.GoBack();
                }
            };

            // Reset the index whenever the ManageLoadout menu is opened. Just to prevent auto selecting the delete option for example.
            ManageLoadoutMenu.OnMenuOpen += (sender) =>
            {
                ManageLoadoutMenu.RefreshIndex();
                var kvp = GetResourceKvpString("vmenu_string_default_loadout");
                if (string.IsNullOrEmpty(kvp) || kvp != SelectedSavedLoadoutName)
                {
                    setDefaultLoadout.LeftIcon = MenuItem.Icon.NONE;
                }
                else
                {
                    setDefaultLoadout.LeftIcon = MenuItem.Icon.TICK;
                }

            };

            // Refresh the saved weapons menu.
            RefreshSavedWeaponsMenu();
        }

        /// <summary>
        /// Gets the menu.
        /// </summary>
        /// <returns></returns>
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
