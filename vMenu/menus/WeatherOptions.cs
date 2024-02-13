using System.Collections.Generic;

using CitizenFX.Core;

using MenuAPI;

using vMenuShared;

using static vMenuClient.CommonFunctions;
using static vMenuShared.PermissionsManager;

namespace vMenuClient.menus
{
    public class WeatherOptions
    {
        // Variables
        private Menu menu;
        public MenuCheckboxItem dynamicWeatherEnabled;
        public MenuCheckboxItem blackout;
        public MenuCheckboxItem snowEnabled;
        public static readonly List<string> weatherTypes = new()
        {
            "EXTRASUNNY",
            "CLEAR",
            "NEUTRAL",
            "SMOG",
            "FOGGY",
            "CLOUDS",
            "OVERCAST",
            "CLEARING",
            "RAIN",
            "THUNDER",
            "BLIZZARD",
            "SNOW",
            "SNOWLIGHT",
            "XMAS",
            "HALLOWEEN"
        };

        private void CreateMenu()
        {
            // Create the menu.
            menu = new Menu(Game.Player.Name, "Weather Options");

            dynamicWeatherEnabled = new MenuCheckboxItem("Activer la météo dynamique", "Activer ou désactiver les changements dynamique de météo.", EventManager.DynamicWeatherEnabled);
            blackout = new MenuCheckboxItem("Activer le Blackout", "Cela active ou désactive toutes les lumières de la map.", EventManager.IsBlackoutEnabled);
            snowEnabled = new MenuCheckboxItem("Activer la neige", "Cela force la neige à apparaître au sol et active les effets qui y sont dédiés.", ConfigManager.GetSettingsBool(ConfigManager.Setting.vmenu_enable_snow));
            var extrasunny = new MenuItem("Grand Soleil", "Changer le temps pour ~y~Grand Soleil~s~!") { ItemData = "EXTRASUNNY" };
            var clear = new MenuItem("Ciel dégagé", "Changer le temps pour ~y~Ciel dégagé~s~!") { ItemData = "CLEAR" };
            var neutral = new MenuItem("Temps moyen", "Changer le temps pour ~y~Temps moyen~s~!") { ItemData = "NEUTRAL" };
            var smog = new MenuItem("Brume", "Changer le temps pour ~y~Brume~s~!") { ItemData = "SMOG" };
            var foggy = new MenuItem("Brouillard", "Changer le temps pour ~y~Brouillard~s~!") { ItemData = "FOGGY" };
            var clouds = new MenuItem("Nuageux", "Changer le temps pour ~y~Nuageux~s~!") { ItemData = "CLOUDS" };
            var overcast = new MenuItem("Très nuageux", "Changer le temps pour ~y~Très nuageux~s~!") { ItemData = "OVERCAST" };
            var clearing = new MenuItem("Eclaircies", "Changer le temps pour ~y~Eclaircies~s~!") { ItemData = "CLEARING" };
            var rain = new MenuItem("Pluie", "Changer le temps pour ~y~Pluie~s~!") { ItemData = "RAIN" };
            var thunder = new MenuItem("Orage", "Changer le temps pour ~y~Orage~s~!") { ItemData = "THUNDER" };
            var blizzard = new MenuItem("Blizzard", "Changer le temps pour ~y~Blizzard~s~!") { ItemData = "BLIZZARD" };
            var snow = new MenuItem("Neige", "Changer le temps pour ~y~Neige~s~!") { ItemData = "SNOW" };
            var snowlight = new MenuItem("Neige faible", "Changer le temps pour ~y~Neige faible~s~!") { ItemData = "SNOWLIGHT" };
            var xmas = new MenuItem("Neige de Noël", "Changer le temps pour ~y~Neige de Noël~s~!") { ItemData = "XMAS" };
            var halloween = new MenuItem("Halloween", "Changer le temps pour ~y~halloween~s~!") { ItemData = "HALLOWEEN" };
            var removeclouds = new MenuItem("Retirer les nuages", "Supprimer tous les nuages du ciel");
            var randomizeclouds = new MenuItem("Nuages aléatoires", "Ajouter des nuages de manière aléatoire");

            if (IsAllowed(Permission.WODynamic))
            {
                menu.AddMenuItem(dynamicWeatherEnabled);
            }
            if (IsAllowed(Permission.WOBlackout))
            {
                menu.AddMenuItem(blackout);
            }
            if (IsAllowed(Permission.WOSetWeather))
            {
                menu.AddMenuItem(snowEnabled);
                menu.AddMenuItem(extrasunny);
                menu.AddMenuItem(clear);
                menu.AddMenuItem(neutral);
                menu.AddMenuItem(smog);
                menu.AddMenuItem(foggy);
                menu.AddMenuItem(clouds);
                menu.AddMenuItem(overcast);
                menu.AddMenuItem(clearing);
                menu.AddMenuItem(rain);
                menu.AddMenuItem(thunder);
                menu.AddMenuItem(blizzard);
                menu.AddMenuItem(snow);
                menu.AddMenuItem(snowlight);
                menu.AddMenuItem(xmas);
                menu.AddMenuItem(halloween);
            }
            if (IsAllowed(Permission.WORandomizeClouds))
            {
                menu.AddMenuItem(randomizeclouds);
            }

            if (IsAllowed(Permission.WORemoveClouds))
            {
                menu.AddMenuItem(removeclouds);
            }

            menu.OnItemSelect += (sender, item, index2) =>
            {
                if (item == removeclouds)
                {
                    ModifyClouds(true);
                }
                else if (item == randomizeclouds)
                {
                    ModifyClouds(false);
                }
                else if (item.ItemData is string weatherType)
                {
                    Notify.Custom($"La météo va être changée pour : ~y~{item.Text}~s~. Cela prendra {EventManager.WeatherChangeTime} secondes.");
                    UpdateServerWeather(weatherType, EventManager.IsBlackoutEnabled, EventManager.DynamicWeatherEnabled, EventManager.IsSnowEnabled);
                }
            };

            menu.OnCheckboxChange += (sender, item, index, _checked) =>
            {
                if (item == dynamicWeatherEnabled)
                {
                    Notify.Custom($"La météo dynamique est maintenant {(_checked ? "~g~activée" : "~r~désactivée")}~s~.");
                    UpdateServerWeather(EventManager.GetServerWeather, EventManager.IsBlackoutEnabled, _checked, EventManager.IsSnowEnabled);
                }
                else if (item == blackout)
                {
                    Notify.Custom($"Le mode Blackout est maintenant {(_checked ? "~g~activé" : "~r~désactivé")}~s~.");
                    UpdateServerWeather(EventManager.GetServerWeather, _checked, EventManager.DynamicWeatherEnabled, EventManager.IsSnowEnabled);
                }
                else if (item == snowEnabled)
                {
                    Notify.Custom($"La neige forcé est maintenant {(_checked ? "~g~activée" : "~r~désactivée")}~s~.");
                    UpdateServerWeather(EventManager.GetServerWeather, EventManager.IsBlackoutEnabled, EventManager.DynamicWeatherEnabled, _checked);
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
    }
}
