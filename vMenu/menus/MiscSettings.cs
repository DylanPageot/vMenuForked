using System;
using System.Collections.Generic;
using System.Linq;

using CitizenFX.Core;

using MenuAPI;

using Newtonsoft.Json;

using vMenuClient.data;

using static CitizenFX.Core.Native.API;
using static vMenuClient.CommonFunctions;
using static vMenuShared.PermissionsManager;

namespace vMenuClient.menus
{
    public class MiscSettings
    {
        // Variables
        private Menu menu;
        private Menu teleportOptionsMenu;
        private Menu developerToolsMenu;
        private Menu entitySpawnerMenu;

        public bool ShowSpeedoKmh { get; private set; } = UserDefaults.MiscSpeedKmh;
        public bool ShowSpeedoMph { get; private set; } = UserDefaults.MiscSpeedMph;
        public bool ShowCoordinates { get; private set; } = false;
        public bool HideHud { get; private set; } = false;
        public bool HideRadar { get; private set; } = false;
        public bool ShowLocation { get; private set; } = UserDefaults.MiscShowLocation;
        public bool DeathNotifications { get; private set; } = UserDefaults.MiscDeathNotifications;
        public bool JoinQuitNotifications { get; private set; } = UserDefaults.MiscJoinQuitNotifications;
        public bool LockCameraX { get; private set; } = false;
        public bool LockCameraY { get; private set; } = false;
        public bool ShowLocationBlips { get; private set; } = UserDefaults.MiscLocationBlips;
        public bool ShowPlayerBlips { get; private set; } = UserDefaults.MiscShowPlayerBlips;
        public bool MiscShowOverheadNames { get; private set; } = UserDefaults.MiscShowOverheadNames;
        public bool ShowVehicleModelDimensions { get; private set; } = false;
        public bool ShowPedModelDimensions { get; private set; } = false;
        public bool ShowPropModelDimensions { get; private set; } = false;
        public bool ShowEntityHandles { get; private set; } = false;
        public bool ShowEntityModels { get; private set; } = false;
        public bool ShowEntityNetOwners { get; private set; } = false;
        public bool MiscRespawnDefaultCharacter { get; private set; } = UserDefaults.MiscRespawnDefaultCharacter;
        public bool RestorePlayerAppearance { get; private set; } = UserDefaults.MiscRestorePlayerAppearance;
        public bool RestorePlayerWeapons { get; private set; } = UserDefaults.MiscRestorePlayerWeapons;
        public bool DrawTimeOnScreen { get; internal set; } = UserDefaults.MiscShowTime;
        public bool MiscRightAlignMenu { get; private set; } = UserDefaults.MiscRightAlignMenu;
        public bool MiscDisablePrivateMessages { get; private set; } = UserDefaults.MiscDisablePrivateMessages;
        public bool MiscDisableControllerSupport { get; private set; } = UserDefaults.MiscDisableControllerSupport;

        internal bool TimecycleEnabled { get; private set; } = false;
        internal int LastTimeCycleModifierIndex { get; private set; } = UserDefaults.MiscLastTimeCycleModifierIndex;
        internal int LastTimeCycleModifierStrength { get; private set; } = UserDefaults.MiscLastTimeCycleModifierStrength;


        // keybind states
        public bool KbTpToWaypoint { get; private set; } = UserDefaults.KbTpToWaypoint;
        public int KbTpToWaypointKey { get; } = vMenuShared.ConfigManager.GetSettingsInt(vMenuShared.ConfigManager.Setting.vmenu_teleport_to_wp_keybind_key) != -1
            ? vMenuShared.ConfigManager.GetSettingsInt(vMenuShared.ConfigManager.Setting.vmenu_teleport_to_wp_keybind_key)
            : 168; // 168 (F7 by default)
        public bool KbDriftMode { get; private set; } = UserDefaults.KbDriftMode;
        public bool KbRecordKeys { get; private set; } = UserDefaults.KbRecordKeys;
        public bool KbRadarKeys { get; private set; } = UserDefaults.KbRadarKeys;
        public bool KbPointKeys { get; private set; } = UserDefaults.KbPointKeys;

        internal static List<vMenuShared.ConfigManager.TeleportLocation> TpLocations = new();

        /// <summary>
        /// Creates the menu.
        /// </summary>
        private void CreateMenu()
        {
            MenuController.MenuAlignment = MiscRightAlignMenu ? MenuController.MenuAlignmentOption.Right : MenuController.MenuAlignmentOption.Left;
            if (MenuController.MenuAlignment != (MiscRightAlignMenu ? MenuController.MenuAlignmentOption.Right : MenuController.MenuAlignmentOption.Left))
            {
                Notify.Error(CommonErrors.RightAlignedNotSupported);

                // (re)set the default to left just in case so they don't get this error again in the future.
                MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Left;
                MiscRightAlignMenu = false;
                UserDefaults.MiscRightAlignMenu = false;
            }

            // Create the menu.
            menu = new Menu(Game.Player.Name, "Autres paramètres");
            teleportOptionsMenu = new Menu(Game.Player.Name, "Options de téléportations");
            developerToolsMenu = new Menu(Game.Player.Name, "Outils de développement");
            entitySpawnerMenu = new Menu(Game.Player.Name, "Spawn d'entités");

            // teleport menu
            var teleportMenu = new Menu(Game.Player.Name, "Lieux de téléportations");
            var teleportMenuBtn = new MenuItem("Lieux de téléportations", "Vous téléporter sur une des positions pré-enregistrées");
            MenuController.AddSubmenu(menu, teleportMenu);
            MenuController.BindMenuItem(menu, teleportMenu, teleportMenuBtn);

            // keybind settings menu
            var keybindMenu = new Menu(Game.Player.Name, "Paramétrage de touches");
            var keybindMenuBtn = new MenuItem("Paramétrage des touches", "Activer ou désactiver des assignations de touches");
            MenuController.AddSubmenu(menu, keybindMenu);
            MenuController.BindMenuItem(menu, keybindMenu, keybindMenuBtn);

            // keybind settings menu items
            var kbTpToWaypoint = new MenuCheckboxItem("Se téléporter à un point", "Se téléporter au point GPS renseigné sur la carte.", KbTpToWaypoint);
            var kbDriftMode = new MenuCheckboxItem("Mode Drift", "Retirer la quasi totalité de la traction de votre véhicule et effectuer de grands dérapages (Touche : X).", KbDriftMode);
            var kbRecordKeys = new MenuCheckboxItem("Options d'enregistrement", "Activer ou désactiver l'enregistrement vidéo de vos sessions de jeux (via le Rockstar Editor).", KbRecordKeys);
            var kbRadarKeys = new MenuCheckboxItem("Contrôles de la Minimap", "Appuyez sur la W (ou flèche de droite sur manette) pour changer le statut d'affichage de la carte.", KbRadarKeys);
            var kbPointKeysCheckbox = new MenuCheckboxItem("Pointer du doigt", "Active ou désactive la possibilité de pointer du doigt. Appuyer sur B pour démarrer/stopper cette action.", KbPointKeys);
            var backBtn = new MenuItem("Back");

            // Create the menu items.
            var rightAlignMenu = new MenuCheckboxItem("Aligner le menu à droite", "Permet d'aligner tout le vMenu sur la droite de l'écran.", MiscRightAlignMenu);
            var disablePms = new MenuCheckboxItem("Désactiver les messages privés", "Désactiver la possibilité pour les autres joueurs de vous envoyer des messages privés en jeu.", MiscDisablePrivateMessages);
            var disableControllerKey = new MenuCheckboxItem("Désactiver la manette", "Désactiver la possibilité d'utiliser votre manette en jeu.", MiscDisableControllerSupport);
            var speedKmh = new MenuCheckboxItem("Afficher la vitesse - KM/H", "Afficher un indicateur de vitesse, en kilomètres par heure.", ShowSpeedoKmh);
            var speedMph = new MenuCheckboxItem("Afficher la vitesse - M/H", "Afficher un indicateur de vitesse, en miles par heure.", ShowSpeedoMph);
            var coords = new MenuCheckboxItem("Afficher les coordonnées", "Afficher vos coordonnées en haut de l'écran.", ShowCoordinates);
            var hideRadar = new MenuCheckboxItem("Cacher la minimap", "Afficher / Cacher la minimap.", HideRadar);
            var hideHud = new MenuCheckboxItem("Cacher l'HUD", "Cacher et désactiver tous les éléments d'HUD.", HideHud);
            var showLocation = new MenuCheckboxItem("Afficher la localisation", "Afficher votre localisation actuelle sur la carte (ex : nom de la rue).", ShowLocation) { LeftIcon = MenuItem.Icon.WARNING };
            var drawTime = new MenuCheckboxItem("Afficher l'heure en jeu", "Affiche l'heure actuelle directement en jeu.", DrawTimeOnScreen);
            var saveSettings = new MenuItem("Enregistrer les paramètres", "Sauvegarder ces paramètres pour qu'ils soient automatiquement rechargés lors de vos prochaines connexions.")
            {
                RightIcon = MenuItem.Icon.TICK
            };
            var exportData = new MenuItem("Export/Import Data", "Coming soon (TM): the ability to import and export your saved data.");
            var joinQuitNotifs = new MenuCheckboxItem("Notifications de déco / reco", "Recevoir une notification lorsqu'un joueur se connecte ou se déconnecte.", JoinQuitNotifications);
            var deathNotifs = new MenuCheckboxItem("Notifications de morts", "Recevoir une notification lorsqu'un joueur se fait tuer.", DeathNotifications);
            var nightVision = new MenuCheckboxItem("Activer la vision nocturne", "Activer ou désactiver la vision nocturne.", false);
            var thermalVision = new MenuCheckboxItem("Activer la vision thermique", "Activer ou désactiver la vision thermique.", false);
            var vehModelDimensions = new MenuCheckboxItem("Afficher les dimensions des véhicules", "Afficher un rectangle autour des véhicules se situant près de vous.", ShowVehicleModelDimensions);
            var propModelDimensions = new MenuCheckboxItem("Afficher les dimensions des props", "Afficher un rectangle autour des props se situant près de vous.", ShowPropModelDimensions);
            var pedModelDimensions = new MenuCheckboxItem("Afficher les dimensions des peds", "Afficher un rectangle autour des peds se situant près de vous.", ShowPedModelDimensions);
            var showEntityHandles = new MenuCheckboxItem("Afficher les HANDLES des entités", "Affiche l'handle des entités autour de vous.", ShowEntityHandles);
            var showEntityModels = new MenuCheckboxItem("Afficher les MODELES des entités", "Affiche les modèles des entités autour de vous.", ShowEntityModels);
            var showEntityNetOwners = new MenuCheckboxItem("Afficher les propriétaires (côté serveur) de l'entité", "Affiche le numéro du client gérant l'affichage de cette entitée.", ShowEntityNetOwners);
            var dimensionsDistanceSlider = new MenuSliderItem("Rayon d'affichage des dimensions", "Modifier le rayon d'affichage des informations ci-dessus.", 0, 20, 20, false);

            var clearArea = new MenuItem("Nettoyer la zone", "Réinitialiser l'état de la map, des véhicules PNJ, etc... dans la zone autour de vous.");
            var lockCamX = new MenuCheckboxItem("Vérouiller la rotation horizontale", "Vérouiller la rotation horizontale de la caméra.", false);
            var lockCamY = new MenuCheckboxItem("Vérouiller la rotation verticale", "Vérouiller la rotation verticale de la caméra.", false);

            // Entity spawner
            var spawnNewEntity = new MenuItem("Faire apparaître une nouvelle entité", "Faire apparaître une nouvelle entité");
            var confirmEntityPosition = new MenuItem("Confirmer le positionnement de l'entité", "Arrête la fonctionalité de placement de l'entité");
            var cancelEntity = new MenuItem("Annuler", "Supprime l'entité actuelle et annule son placement.");
            var confirmAndDuplicate = new MenuItem("Confirmer le positionnement et dupliqué", "Arrête la fonctionnalité de placement de l'entité et permet d'en placer une nouvelle.");

            var connectionSubmenu = new Menu(Game.Player.Name, "Options de connexions");
            var connectionSubmenuBtn = new MenuItem("Options de connexions", "Options de (dé)connexion au serveur.");

            var quitSession = new MenuItem("Quitter la session", "Vous garde connecté au serveur mais dans une nouvelle session de jeu.");
            var rejoinSession = new MenuItem("Rejoindre la session", "Cela peut ne pas fonctionner, mais vous permet de rejoindre la session précedemment quittée");
            var quitGame = new MenuItem("Quitter le jeu", "Quitter le jeu au bout de 5 secondes.");
            var disconnectFromServer = new MenuItem("Se déconnecter", "Se déconnecter du serveur. Nous conseillons tout de même de rédémarrer le jeu après cela.");
            connectionSubmenu.AddMenuItem(quitSession);
            connectionSubmenu.AddMenuItem(rejoinSession);
            connectionSubmenu.AddMenuItem(quitGame);
            connectionSubmenu.AddMenuItem(disconnectFromServer);

            var enableTimeCycle = new MenuCheckboxItem("Activer le modificateur de temps", "Active ou désactive le cycle de modification du temps.", TimecycleEnabled);
            var timeCycleModifiersListData = TimeCycles.Timecycles.ToList();
            for (var i = 0; i < timeCycleModifiersListData.Count; i++)
            {
                timeCycleModifiersListData[i] += $" ({i + 1}/{timeCycleModifiersListData.Count})";
            }
            var timeCycles = new MenuListItem("TM", timeCycleModifiersListData, MathUtil.Clamp(LastTimeCycleModifierIndex, 0, Math.Max(0, timeCycleModifiersListData.Count - 1)), "Sélectionner un modificateur dans la liste ici présente.");
            var timeCycleIntensity = new MenuSliderItem("Intensité du modificateur", "Modifier l'intensité du modificateur.", 0, 20, LastTimeCycleModifierStrength, true);

            var locationBlips = new MenuCheckboxItem("Points importants", "Afficher les points les plus importants sur la carte.", ShowLocationBlips);
            var playerBlips = new MenuCheckboxItem("Afficher les points des joueurs", "Afficher les points des joueurs à proximité.", ShowPlayerBlips);
            var playerNames = new MenuCheckboxItem("Afficher les noms des joueurs", "Afficher les noms des joueurs au dessus de leurs têtes", MiscShowOverheadNames);
            var respawnDefaultCharacter = new MenuCheckboxItem("Réapparaître avec son MP PED par défaut", "Si activé, vous réapparaitrez avec votre personnage par défaut, préalablement renseigné.", MiscRespawnDefaultCharacter);
            var restorePlayerAppearance = new MenuCheckboxItem("Restaurer l'apparence du joueur", "Restaure l'apparence de votre personnage lorsque vous mourrez notamment.", RestorePlayerAppearance);
            var restorePlayerWeapons = new MenuCheckboxItem("Restaurer les armes du joueur", "Restaure vos armes lorsque vous mourrez.", RestorePlayerWeapons);

            MenuController.AddSubmenu(menu, connectionSubmenu);
            MenuController.BindMenuItem(menu, connectionSubmenu, connectionSubmenuBtn);

            keybindMenu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                if (item == kbTpToWaypoint)
                {
                    KbTpToWaypoint = _checked;
                }
                else if (item == kbDriftMode)
                {
                    KbDriftMode = _checked;
                }
                else if (item == kbRecordKeys)
                {
                    KbRecordKeys = _checked;
                }
                else if (item == kbRadarKeys)
                {
                    KbRadarKeys = _checked;
                }
                else if (item == kbPointKeysCheckbox)
                {
                    KbPointKeys = _checked;
                }
            };
            keybindMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == backBtn)
                {
                    keybindMenu.GoBack();
                }
            };

            connectionSubmenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == quitGame)
                {
                    QuitGame();
                }
                else if (item == quitSession)
                {
                    if (NetworkIsSessionActive())
                    {
                        if (NetworkIsHost())
                        {
                            Notify.Error("Vous ne pouvez pas quitter la session lorsque vous l'hébergez.");
                        }
                        else
                        {
                            QuitSession();
                        }
                    }
                    else
                    {
                        Notify.Error("Vous n'êts actuellement pas dans une session.");
                    }
                }
                else if (item == rejoinSession)
                {
                    if (NetworkIsSessionActive())
                    {
                        Notify.Error("Vous êtes déjà connectés à la session.");
                    }
                    else
                    {
                        Notify.Info("Tentative de reconnexion à la session...");
                        NetworkSessionHost(-1, 32, false);
                    }
                }
                else if (item == disconnectFromServer)
                {

                    RegisterCommand("disconnect", new Action<dynamic, dynamic, dynamic>((a, b, c) => { }), false);
                    ExecuteCommand("disconnect");
                }
            };

            // Teleportation options
            if (IsAllowed(Permission.MSTeleportToWp) || IsAllowed(Permission.MSTeleportLocations) || IsAllowed(Permission.MSTeleportToCoord))
            {
                var teleportOptionsMenuBtn = new MenuItem("Options de téléportations", "Divers options de téléportations.") { Label = "→→→" };
                menu.AddMenuItem(teleportOptionsMenuBtn);
                MenuController.BindMenuItem(menu, teleportOptionsMenu, teleportOptionsMenuBtn);

                var tptowp = new MenuItem("Se téléporter à un point", "Vous téléporte à un point de la carte.");
                var tpToCoord = new MenuItem("Se téléporter aux coordonnées", "Entrez des coordonnées X, Y, Z où vous téléporter.");
                var saveLocationBtn = new MenuItem("Sauvegarder le point de téléportation", "Ajouter votre localisation à la liste des points de téléportation.");
                teleportOptionsMenu.OnItemSelect += async (sender, item, index) =>
                {
                    // Teleport to waypoint.
                    if (item == tptowp)
                    {
                        TeleportToWp();
                    }
                    else if (item == tpToCoord)
                    {
                        string x = await GetUserInput("Coordonnée X");
                        if (string.IsNullOrEmpty(x))
                        {
                            Notify.Error(CommonErrors.InvalidInput);
                            return;
                        }
                        string y = await GetUserInput("Coordonnée Y");
                        if (string.IsNullOrEmpty(y))
                        {
                            Notify.Error(CommonErrors.InvalidInput);
                            return;
                        }
                        string z = await GetUserInput("Coordonnée Z");
                        if (string.IsNullOrEmpty(z))
                        {
                            Notify.Error(CommonErrors.InvalidInput);
                            return;
                        }


                        if (!float.TryParse(x, out var posX))
                        {
                            if (int.TryParse(x, out var intX))
                            {
                                posX = intX;
                            }
                            else
                            {
                                Notify.Error("Vous avez entré une coordonnée X invalide.");
                                return;
                            }
                        }
                        if (!float.TryParse(y, out var posY))
                        {
                            if (int.TryParse(y, out var intY))
                            {
                                posY = intY;
                            }
                            else
                            {
                                Notify.Error("Vous avez entré une coordonnée Y invalide.");
                                return;
                            }
                        }
                        if (!float.TryParse(z, out var posZ))
                        {
                            if (int.TryParse(z, out var intZ))
                            {
                                posZ = intZ;
                            }
                            else
                            {
                                Notify.Error("Vous avez entré une coordonnée Z invalide.");
                                return;
                            }
                        }

                        await TeleportToCoords(new Vector3(posX, posY, posZ), true);
                    }
                    else if (item == saveLocationBtn)
                    {
                        SavePlayerLocationToLocationsFile();
                    }
                };

                if (IsAllowed(Permission.MSTeleportToWp))
                {
                    teleportOptionsMenu.AddMenuItem(tptowp);
                    keybindMenu.AddMenuItem(kbTpToWaypoint);
                }
                if (IsAllowed(Permission.MSTeleportToCoord))
                {
                    teleportOptionsMenu.AddMenuItem(tpToCoord);
                }
                if (IsAllowed(Permission.MSTeleportLocations))
                {
                    teleportOptionsMenu.AddMenuItem(teleportMenuBtn);

                    MenuController.AddSubmenu(teleportOptionsMenu, teleportMenu);
                    MenuController.BindMenuItem(teleportOptionsMenu, teleportMenu, teleportMenuBtn);
                    teleportMenuBtn.Label = "→→→";

                    teleportMenu.OnMenuOpen += (sender) =>
                    {
                        if (teleportMenu.Size != TpLocations.Count())
                        {
                            teleportMenu.ClearMenuItems();
                            foreach (var location in TpLocations)
                            {
                                var x = Math.Round(location.coordinates.X, 2);
                                var y = Math.Round(location.coordinates.Y, 2);
                                var z = Math.Round(location.coordinates.Z, 2);
                                var heading = Math.Round(location.heading, 2);
                                var tpBtn = new MenuItem(location.name, $"Téléporté au ~y~{location.name}~n~~s~x: ~y~{x}~n~~s~y: ~y~{y}~n~~s~z: ~y~{z}~n~~s~orientation: ~y~{heading}") { ItemData = location };
                                teleportMenu.AddMenuItem(tpBtn);
                            }
                        }
                    };

                    teleportMenu.OnItemSelect += async (sender, item, index) =>
                    {
                        if (item.ItemData is vMenuShared.ConfigManager.TeleportLocation tl)
                        {
                            await TeleportToCoords(tl.coordinates, true);
                            SetEntityHeading(Game.PlayerPed.Handle, tl.heading);
                            SetGameplayCamRelativeHeading(0f);
                        }
                    };

                    if (IsAllowed(Permission.MSTeleportSaveLocation))
                    {
                        teleportOptionsMenu.AddMenuItem(saveLocationBtn);
                    }
                }

            }

            #region dev tools menu

            var devToolsBtn = new MenuItem("Outils de développement", "Divers outils de développement et de debug") { Label = "→→→" };

            // clear area and coordinates
            if (IsAllowed(Permission.MSClearArea))
            {
                developerToolsMenu.AddMenuItem(clearArea);
            
                menu.AddMenuItem(devToolsBtn);
                MenuController.AddSubmenu(menu, developerToolsMenu);
                MenuController.BindMenuItem(menu, developerToolsMenu, devToolsBtn);
            }
            if (IsAllowed(Permission.MSShowCoordinates))
            {
                developerToolsMenu.AddMenuItem(coords);
            }

            // model outlines
            if ((!vMenuShared.ConfigManager.GetSettingsBool(vMenuShared.ConfigManager.Setting.vmenu_disable_entity_outlines_tool)) && (IsAllowed(Permission.MSDevTools)))
            {
                developerToolsMenu.AddMenuItem(vehModelDimensions);
                developerToolsMenu.AddMenuItem(propModelDimensions);
                developerToolsMenu.AddMenuItem(pedModelDimensions);
                developerToolsMenu.AddMenuItem(showEntityHandles);
                developerToolsMenu.AddMenuItem(showEntityModels);
                developerToolsMenu.AddMenuItem(showEntityNetOwners);
                developerToolsMenu.AddMenuItem(dimensionsDistanceSlider);
            }


            // timecycle modifiers
            developerToolsMenu.AddMenuItem(timeCycles);
            developerToolsMenu.AddMenuItem(enableTimeCycle);
            developerToolsMenu.AddMenuItem(timeCycleIntensity);

            developerToolsMenu.OnSliderPositionChange += (sender, item, oldPos, newPos, itemIndex) =>
            {
                if (item == timeCycleIntensity)
                {
                    ClearTimecycleModifier();
                    if (TimecycleEnabled)
                    {
                        SetTimecycleModifier(TimeCycles.Timecycles[timeCycles.ListIndex]);
                        var intensity = newPos / 20f;
                        SetTimecycleModifierStrength(intensity);
                    }
                    UserDefaults.MiscLastTimeCycleModifierIndex = timeCycles.ListIndex;
                    UserDefaults.MiscLastTimeCycleModifierStrength = timeCycleIntensity.Position;
                }
                else if (item == dimensionsDistanceSlider)
                {
                    FunctionsController.entityRange = newPos / 20f * 2000f; // max radius = 2000f;
                }
            };

            developerToolsMenu.OnListIndexChange += (sender, item, oldIndex, newIndex, itemIndex) =>
            {
                if (item == timeCycles)
                {
                    ClearTimecycleModifier();
                    if (TimecycleEnabled)
                    {
                        SetTimecycleModifier(TimeCycles.Timecycles[timeCycles.ListIndex]);
                        var intensity = timeCycleIntensity.Position / 20f;
                        SetTimecycleModifierStrength(intensity);
                    }
                    UserDefaults.MiscLastTimeCycleModifierIndex = timeCycles.ListIndex;
                    UserDefaults.MiscLastTimeCycleModifierStrength = timeCycleIntensity.Position;
                }
            };

            developerToolsMenu.OnItemSelect += (sender, item, index) =>
            {
                if (item == clearArea)
                {
                    var pos = Game.PlayerPed.Position;
                    BaseScript.TriggerServerEvent("vMenu:ClearArea", pos.X, pos.Y, pos.Z);
                }
            };

            developerToolsMenu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                if (item == vehModelDimensions)
                {
                    ShowVehicleModelDimensions = _checked;
                }
                else if (item == propModelDimensions)
                {
                    ShowPropModelDimensions = _checked;
                }
                else if (item == pedModelDimensions)
                {
                    ShowPedModelDimensions = _checked;
                }
                else if (item == showEntityHandles)
                {
                    ShowEntityHandles = _checked;
                }
                else if (item == showEntityModels)
                {
                    ShowEntityModels = _checked;
                }
                else if (item == showEntityNetOwners)
                {
                    ShowEntityNetOwners = _checked;
                }
                else if (item == enableTimeCycle)
                {
                    TimecycleEnabled = _checked;
                    ClearTimecycleModifier();
                    if (TimecycleEnabled)
                    {
                        SetTimecycleModifier(TimeCycles.Timecycles[timeCycles.ListIndex]);
                        var intensity = timeCycleIntensity.Position / 20f;
                        SetTimecycleModifierStrength(intensity);
                    }
                }
                else if (item == coords)
                {
                    ShowCoordinates = _checked;
                }
            };

            if (IsAllowed(Permission.MSEntitySpawner))
            {
                var entSpawnerMenuBtn = new MenuItem("Entity Spawner", "Spawn and move entities") { Label = "→→→" };
                developerToolsMenu.AddMenuItem(entSpawnerMenuBtn);
                MenuController.BindMenuItem(developerToolsMenu, entitySpawnerMenu, entSpawnerMenuBtn);

                entitySpawnerMenu.AddMenuItem(spawnNewEntity);
                entitySpawnerMenu.AddMenuItem(confirmEntityPosition);
                entitySpawnerMenu.AddMenuItem(confirmAndDuplicate);
                entitySpawnerMenu.AddMenuItem(cancelEntity);

                entitySpawnerMenu.OnItemSelect += async (sender, item, index) =>
                {
                    if (item == spawnNewEntity)
                    {
                        if (EntitySpawner.CurrentEntity != null || EntitySpawner.Active)
                        {
                            Notify.Error("You are already placing one entity, set its location or cancel and try again!");
                            return;
                        }

                        var result = await GetUserInput(windowTitle: "Enter model name");

                        if (string.IsNullOrEmpty(result))
                        {
                            Notify.Error(CommonErrors.InvalidInput);
                        }

                        EntitySpawner.SpawnEntity(result, Game.PlayerPed.Position);
                    }
                    else if (item == confirmEntityPosition || item == confirmAndDuplicate)
                    {
                        if (EntitySpawner.CurrentEntity != null)
                        {
                            EntitySpawner.FinishPlacement(item == confirmAndDuplicate);
                        }
                        else
                        {
                            Notify.Error("No entity to confirm position for!");
                        }
                    }
                    else if (item == cancelEntity)
                    {
                        if (EntitySpawner.CurrentEntity != null)
                        {
                            EntitySpawner.CurrentEntity.Delete();
                        }
                        else
                        {
                            Notify.Error("No entity to cancel!");
                        }
                    }
                };
            }

            #endregion


            // Keybind options
            if (IsAllowed(Permission.MSDriftMode))
            {
                keybindMenu.AddMenuItem(kbDriftMode);
            }
            // always allowed keybind menu options
            keybindMenu.AddMenuItem(kbRecordKeys);
            keybindMenu.AddMenuItem(kbRadarKeys);
            keybindMenu.AddMenuItem(kbPointKeysCheckbox);
            keybindMenu.AddMenuItem(backBtn);

            // Always allowed
            menu.AddMenuItem(rightAlignMenu);
            menu.AddMenuItem(disablePms);
            menu.AddMenuItem(disableControllerKey);
            menu.AddMenuItem(speedKmh);
            menu.AddMenuItem(speedMph);
            menu.AddMenuItem(keybindMenuBtn);
            keybindMenuBtn.Label = "→→→";
            if (IsAllowed(Permission.MSConnectionMenu))
            {
                menu.AddMenuItem(connectionSubmenuBtn);
                connectionSubmenuBtn.Label = "→→→";
            }
            if (IsAllowed(Permission.MSShowLocation))
            {
                menu.AddMenuItem(showLocation);
            }
            menu.AddMenuItem(drawTime); // always allowed
            if (IsAllowed(Permission.MSJoinQuitNotifs))
            {
                menu.AddMenuItem(joinQuitNotifs);
            }
            if (IsAllowed(Permission.MSDeathNotifs))
            {
                menu.AddMenuItem(deathNotifs);
            }
            if (IsAllowed(Permission.MSNightVision))
            {
                menu.AddMenuItem(nightVision);
            }
            if (IsAllowed(Permission.MSThermalVision))
            {
                menu.AddMenuItem(thermalVision);
            }
            if (IsAllowed(Permission.MSLocationBlips))
            {
                menu.AddMenuItem(locationBlips);
                ToggleBlips(ShowLocationBlips);
            }
            if (IsAllowed(Permission.MSPlayerBlips))
            {
                menu.AddMenuItem(playerBlips);
            }
            if (IsAllowed(Permission.MSOverheadNames))
            {
                menu.AddMenuItem(playerNames);
            }
            // always allowed, it just won't do anything if the server owner disabled the feature, but players can still toggle it.
            menu.AddMenuItem(respawnDefaultCharacter);
            if (IsAllowed(Permission.MSRestoreAppearance))
            {
                menu.AddMenuItem(restorePlayerAppearance);
            }
            if (IsAllowed(Permission.MSRestoreWeapons))
            {
                menu.AddMenuItem(restorePlayerWeapons);
            }

            // Always allowed
            menu.AddMenuItem(hideRadar);
            menu.AddMenuItem(hideHud);
            menu.AddMenuItem(lockCamX);
            menu.AddMenuItem(lockCamY);
            if (MainMenu.EnableExperimentalFeatures)
            {
                menu.AddMenuItem(exportData);
            }
            menu.AddMenuItem(saveSettings);

            // Handle checkbox changes.
            menu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                if (item == rightAlignMenu)
                {

                    MenuController.MenuAlignment = _checked ? MenuController.MenuAlignmentOption.Right : MenuController.MenuAlignmentOption.Left;
                    MiscRightAlignMenu = _checked;
                    UserDefaults.MiscRightAlignMenu = MiscRightAlignMenu;

                    if (MenuController.MenuAlignment != (_checked ? MenuController.MenuAlignmentOption.Right : MenuController.MenuAlignmentOption.Left))
                    {
                        Notify.Error(CommonErrors.RightAlignedNotSupported);
                        // (re)set the default to left just in case so they don't get this error again in the future.
                        MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Left;
                        MiscRightAlignMenu = false;
                        UserDefaults.MiscRightAlignMenu = false;
                    }

                }
                else if (item == disablePms)
                {
                    MiscDisablePrivateMessages = _checked;
                }
                else if (item == disableControllerKey)
                {
                    MiscDisableControllerSupport = _checked;
                    MenuController.EnableMenuToggleKeyOnController = !_checked;
                }
                else if (item == speedKmh)
                {
                    ShowSpeedoKmh = _checked;
                }
                else if (item == speedMph)
                {
                    ShowSpeedoMph = _checked;
                }
                else if (item == hideHud)
                {
                    HideHud = _checked;
                    DisplayHud(!_checked);
                }
                else if (item == hideRadar)
                {
                    HideRadar = _checked;
                    if (!_checked)
                    {
                        DisplayRadar(true);
                    }
                }
                else if (item == showLocation)
                {
                    ShowLocation = _checked;
                }
                else if (item == drawTime)
                {
                    DrawTimeOnScreen = _checked;
                }
                else if (item == deathNotifs)
                {
                    DeathNotifications = _checked;
                }
                else if (item == joinQuitNotifs)
                {
                    JoinQuitNotifications = _checked;
                }
                else if (item == nightVision)
                {
                    SetNightvision(_checked);
                }
                else if (item == thermalVision)
                {
                    SetSeethrough(_checked);
                }
                else if (item == lockCamX)
                {
                    LockCameraX = _checked;
                }
                else if (item == lockCamY)
                {
                    LockCameraY = _checked;
                }
                else if (item == locationBlips)
                {
                    ToggleBlips(_checked);
                    ShowLocationBlips = _checked;
                }
                else if (item == playerBlips)
                {
                    ShowPlayerBlips = _checked;
                }
                else if (item == playerNames)
                {
                    MiscShowOverheadNames = _checked;
                }
                else if (item == respawnDefaultCharacter)
                {
                    MiscRespawnDefaultCharacter = _checked;
                }
                else if (item == restorePlayerAppearance)
                {
                    RestorePlayerAppearance = _checked;
                }
                else if (item == restorePlayerWeapons)
                {
                    RestorePlayerWeapons = _checked;
                }

            };

            // Handle button presses.
            menu.OnItemSelect += (sender, item, index) =>
            {
                // export data
                if (item == exportData)
                {
                    MenuController.CloseAllMenus();
                    var vehicles = GetSavedVehicles();
                    var normalPeds = StorageManager.GetSavedPeds();
                    var mpPeds = StorageManager.GetSavedMpPeds();
                    var weaponLoadouts = WeaponLoadouts.GetSavedWeapons();
                    var data = JsonConvert.SerializeObject(new
                    {
                        saved_vehicles = vehicles,
                        normal_peds = normalPeds,
                        mp_characters = mpPeds,
                        weapon_loadouts = weaponLoadouts
                    });
                    SendNuiMessage(data);
                    SetNuiFocus(true, true);
                }
                // save settings
                else if (item == saveSettings)
                {
                    UserDefaults.SaveSettings();
                }
            };
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

        private readonly struct Blip
        {
            public readonly Vector3 Location;
            public readonly int Sprite;
            public readonly string Name;
            public readonly int Color;
            public readonly int blipID;

            public Blip(Vector3 Location, int Sprite, string Name, int Color, int blipID)
            {
                this.Location = Location;
                this.Sprite = Sprite;
                this.Name = Name;
                this.Color = Color;
                this.blipID = blipID;
            }
        }

        private readonly List<Blip> blips = new();

        /// <summary>
        /// Toggles blips on/off.
        /// </summary>
        /// <param name="enable"></param>
        private void ToggleBlips(bool enable)
        {
            if (enable)
            {
                try
                {
                    foreach (var bl in vMenuShared.ConfigManager.GetLocationBlipsData())
                    {
                        var blipID = AddBlipForCoord(bl.coordinates.X, bl.coordinates.Y, bl.coordinates.Z);
                        SetBlipSprite(blipID, bl.spriteID);
                        BeginTextCommandSetBlipName("STRING");
                        AddTextComponentSubstringPlayerName(bl.name);
                        EndTextCommandSetBlipName(blipID);
                        SetBlipColour(blipID, bl.color);
                        SetBlipAsShortRange(blipID, true);

                        var b = new Blip(bl.coordinates, bl.spriteID, bl.name, bl.color, blipID);
                        blips.Add(b);
                    }
                }
                catch (JsonReaderException ex)
                {
                    Debug.Write($"\n\n[vMenu] An error occurred while loading the locations.json file. Please contact the server owner to resolve this.\nWhen contacting the owner, provide the following error details:\n{ex.Message}.\n\n\n");
                }
            }
            else
            {
                if (blips.Count > 0)
                {
                    foreach (var blip in blips)
                    {
                        var id = blip.blipID;
                        if (DoesBlipExist(id))
                        {
                            RemoveBlip(ref id);
                        }
                    }
                }
                blips.Clear();
            }
        }

    }
}
