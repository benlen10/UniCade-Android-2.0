﻿using Android.App;
using Android.Content;
using Android.OS;
using Android.Widget;
using UniCadeAndroid.Backend;
using System.Collections.Generic;
using System.Linq;
using Android;
using Android.Content.PM;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Views;
using Java.Lang;
using UniCadeAndroid.Objects;

namespace UniCadeAndroid.Activities
{
    [Activity(Label = "UniCade Mobile", MainLauncher = true, Icon = "@drawable/UniCadeIcon")]
    public class MainActivity : Activity
    {

        #region Private Instance Variables 

        private Button _settingsButton;

        private CheckBox _showFavoritesCheckbox;

        private CheckBox _globalSearchCheckbox;

        private ListView _gameSelectionListView;

        private Spinner _consoleSelectionSpinner;

        private EditText _searchBarEditText;

        private ImageView _consoleImageView;

        private bool _favoritesViewEnabled;

        private bool _globalSearchEnabled;

        private List<GameListObject> _currentGameList;

        string[] requiredPermissions =
        {
            Manifest.Permission.ReadExternalStorage,
            Manifest.Permission.WriteExternalStorage
        };


        #endregion

        #region Properties

        /// <summary>
        /// The currently selected IGame object
        /// </summary>
        public static Game CurrentGame;

        #endregion

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            CheckPermissions();
        }

        private void Startup()
        {
            //Initalize the database, preform an initial scan and refresh the total game count
            Database.Initalize();

            //Validate the media directory and attempt to laod both the database.xml and preferences.xml files
            if (!FileOps.StartupScan())
            {
                return;
            }

            //Refresh the total came count across all consoles
            Database.RefreshTotalGameCount();

            SetContentView(Resource.Layout.MainView);

            FindElementsById();

            CreateEventHandlers();

            PopulateConsoleSpinner();
        }

        public void PopulateConsoleSpinner()
        {

            List<string> consoleList = Database.GetConsoleList().ToList();

            var consoleSpinnerAdapter =
                new ArrayAdapter(this, Resource.Layout.CustomSpinnerItem, consoleList);

            _consoleSelectionSpinner.Adapter = consoleSpinnerAdapter;
        }

        private void CheckPermissions()
        {

            if (ContextCompat.CheckSelfPermission(this, requiredPermissions[0]) == (int) Permission.Granted)
            {
                Startup();
            }
            else
            {
                ActivityCompat.RequestPermissions(this, requiredPermissions, 0);
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (ContextCompat.CheckSelfPermission(this, requiredPermissions[0]) == (int) Permission.Granted)
            {
                Startup();
            }
            else
            {
                Toast.MakeText(ApplicationContext, "Fatal Error: Storage Access Required", ToastLength.Long).Show();
                Finish();
            }

        }

        private void RefreshGameList()
        {

            _gameSelectionListView.Adapter = null;
            var currentConsoleName = _consoleSelectionSpinner.SelectedItem.ToString();
            _currentGameList = new List<GameListObject>();

            if (currentConsoleName == "All Games")
            {
                foreach (string consoleName in Database.GetConsoleList())
                {
                    foreach (string gameTitle in Database.GetConsole(consoleName).GetGameList())
                    {
                        var item = new GameListObject
                        {
                            Title = gameTitle,
                            Console = currentConsoleName,
                            ImageResourceId = 0
                        };
                        _currentGameList.Add(item);
                    }
                }
            }
            else
            {
                foreach (var gameTitle in Database.GetConsole(currentConsoleName).GetGameList())
                {
                    var item = new GameListObject
                    {
                        Title = gameTitle,
                        Console = currentConsoleName,
                        ImageResourceId = 0
                    };
                    _currentGameList.Add(item);
                }
            }

            var gameListAdapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleListItemActivated2, _currentGameList);
            _gameSelectionListView.Adapter = gameListAdapter;

            _gameSelectionListView.ChoiceMode = ChoiceMode.Single;

            _gameSelectionListView.Adapter = new GameListViewActivity(this, _currentGameList);
            _gameSelectionListView.ItemClick += OnListItemClick;
        }

        void OnListItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            var listView = sender as ListView;
            if (listView != null)
            {
                var listItem = _currentGameList[e.Position];

                CurrentGame = (Game) Database.GetConsole(listItem.Console).GetGame(listItem.Title);

                if (CurrentGame != null)
                {
                    var intent = new Intent(this, typeof(GameInfoActivity));
                    StartActivity(intent);
                }
                else
                {
                    Toast.MakeText(ApplicationContext, "Please select a game", ToastLength.Long).Show();
                }
            }

        }

        private void FindElementsById()
        {
            _settingsButton = FindViewById<Button>(Resource.Id.SettingsButton);
            _showFavoritesCheckbox = FindViewById<CheckBox>(Resource.Id.ShowFavoritesCheckbox);
            _globalSearchCheckbox = FindViewById<CheckBox>(Resource.Id.GlobalfavoritesCheckbox);
            _gameSelectionListView = FindViewById<ListView>(Resource.Id.GameSelectionListView);
            _consoleSelectionSpinner = FindViewById<Spinner>(Resource.Id.ConsoleSelectionSpinner);
            _searchBarEditText = FindViewById<EditText>(Resource.Id.SearchBarEditTExt);
            _consoleImageView = FindViewById<ImageView>(Resource.Id.ConsoleImageView);
        }

        private void SearchBarTextChanged()
        {
            _gameSelectionListView.Adapter = null;
            if (_searchBarEditText.Text.Length > 0)
            {
                var currentSearchText = _searchBarEditText.Text;
                var currentConsole = _consoleSelectionSpinner.SelectedItem.ToString();
                var gameList = Database.GetConsole(currentConsole).GetGameList().Where(gameName => gameName.Contains(currentSearchText)).ToList();
                var gameListAdapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleSpinnerItem, gameList);
                _gameSelectionListView.Adapter = gameListAdapter;
            }
            else
            {
                RefreshGameList();
            }
        }

        private void CreateEventHandlers()
        {
            _settingsButton.Click += (sender, e) =>
            {
                var intent = new Intent(this, typeof(SettingsActivity));
                StartActivity(intent);
            };

            _consoleSelectionSpinner.ItemSelected += (sender, e) =>
            {
                RefreshGameList();
            };

            _showFavoritesCheckbox.CheckedChange += (sender, e) =>
            {
                _favoritesViewEnabled = _showFavoritesCheckbox.Checked;
            };

            _globalSearchCheckbox.CheckedChange += (sender, e) =>
            {
                _globalSearchEnabled = _globalSearchCheckbox.Checked;
            };

            _searchBarEditText.TextChanged += (sender, e) =>
            {
                SearchBarTextChanged();
            };

          
        }
    }
}

