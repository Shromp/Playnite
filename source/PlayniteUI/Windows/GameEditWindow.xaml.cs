﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using NLog;
using Playnite;
using Playnite.Database;
using Playnite.Models;
using PlayniteUI.Windows;
using Playnite.Providers.Steam;
using Playnite.Providers.GOG;
using Playnite.Providers.Origin;
using PlayniteUI.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace PlayniteUI
{
    /// <summary>
    /// Interaction logic for GameEditWindow.xaml
    /// </summary>
    public partial class GameEditWindow : WindowBase, INotifyPropertyChanged
    {        
        private static Logger logger = LogManager.GetCurrentClassLogger();
        private WindowPositionHandler positionManager;

        private IGame game;
        public IGame Game
        {
            get
            {
                return game;
            }

            set
            {
                game = value;
                DataChanged(game);
            }
        }

        private IEnumerable<IGame> games;
        public IEnumerable<IGame> Games
        {
            get
            {
                return games;
            }

            set
            {
                games = value;
                DataChanged(games);
            }
        }

        private List<Emulator> emulators;
        public List<Emulator> Emulators
        {
            get
            {
                return emulators;
            }

            set
            {
                emulators = value;
                OnPropertyChanged("Emulators");
            }
        }

        private bool checkBoxesVisible = false;
        public bool CheckBoxesVisible
        {
            get
            {
                return checkBoxesVisible;
            }

            set
            {
                checkBoxesVisible = value;
                OnPropertyChanged("CheckBoxesVisible");
            }
        }

        private GameTask tempPlayTask;
        public GameTask TempPlayTask
        {
            get
            {
                return tempPlayTask;
            }

            set
            {
                tempPlayTask = value;
                OnPropertyChanged("ShowAddPlayAction");
                OnPropertyChanged("ShowRemovePlayAction");
                OnPropertyChanged("TempPlayTask");
                OnPropertyChanged("ShowPlayActionEdit");
            }
        }

        private ObservableCollection<GameTask> tempOtherTasks;
        public ObservableCollection<GameTask> TempOtherTasks
        {
            get
            {
                return tempOtherTasks;
            }

            set
            {
                tempOtherTasks = value;
                OnPropertyChanged("TempOtherTasks");
            }
        }

        private ObservableCollection<Link> tempLinks;
        public ObservableCollection<Link> TempLinks
        {
            get
            {
                return tempLinks;
            }

            set
            {
                tempLinks = value;
                OnPropertyChanged("TempLinks");
            }
        }

        public bool ShowPlayActionEdit
        {
            get
            {

                return TempPlayTask != null;
            }
        }

        public bool ShowAddPlayAction
        {
            get
            {
                if (DataContext == null)
                {
                    return false;
                }

                if (Game.Provider != Provider.Custom)
                {
                    return false;
                }

                return TempPlayTask == null;
            }
        }

        public bool ShowRemovePlayAction
        {
            get
            {
                if (DataContext == null)
                {
                    return false;
                }

                if (Game.Provider != Provider.Custom)
                {
                    return false;
                }

                return TempPlayTask != null && !TempPlayTask.IsBuiltIn;
            }
        }

        #region Dirty flags
        public bool IsNameBindingDirty
        {
            get
            {
                return IsControlBindingDirty(TextName, TextBox.TextProperty);
            }
        }

        public bool IsPlatformBindingDirty
        {
            get
            {
                return IsControlBindingDirty(ComboPlatforms, ComboBox.SelectedValueProperty);
            }
        }

        public bool IsGenreBindingDirty
        {
            get
            {
                return IsControlBindingDirty(TextGenres, TextBox.TextProperty);
            }
        }

        public bool IsReleaseDateBindingDirty
        {
            get
            {
                return IsControlBindingDirty(TextReleaseDate, TextBox.TextProperty);
            }
        }

        public bool IsDeveloperBindingDirty
        {
            get
            {
                return IsControlBindingDirty(TextDeveloper, TextBox.TextProperty);
            }
        }

        public bool IsPublisherBindingDirty
        {
            get
            {
                return IsControlBindingDirty(TextPublisher, TextBox.TextProperty);
            }
        }

        public bool IsCategoriesBindingDirty
        {
            get
            {
                return IsControlBindingDirty(TextCategories, TextBox.TextProperty);
            }
        }        

        public bool IsDescriptionBindingDirty
        {
            get
            {
                return IsControlBindingDirty(TextDescription, TextBox.TextProperty);
            }
        }

        public bool IsIconBindingDirty
        {
            get
            {
                return IsControlBindingDirty(ImageIcon, Image.SourceProperty);
            }
        }

        public bool IsImageBindingDirty
        {
            get
            {
                return IsControlBindingDirty(ImageImage, Image.SourceProperty);
            }
        }

        public bool IsInstallDirBindingDirty
        {
            get
            {
                return IsControlBindingDirty(TextInstallDir, TextBox.TextProperty);
            }
        }

        public bool IsIsoPathBindingDirty
        {
            get
            {
                return IsControlBindingDirty(TextIso, TextBox.TextProperty);
            }
        }

        #endregion Dirty flags

        private CheckBox CheckIcon;
        private CheckBox CheckImage;

        public event PropertyChangedEventHandler PropertyChanged;

        public GameEditWindow()
        {
            InitializeComponent();
            positionManager = new WindowPositionHandler(this, "EditGame", Settings.Instance);
        }

        public void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private bool IsControlBindingDirty(DependencyObject control, DependencyProperty property)
        {
            if (control == null)
            {
                return false;
            }

            return BindingOperations.GetBindingExpression(control, property).IsDirty;
        }



        private void DataChanged(object data)
        {
            if (data is IEnumerable<IGame>)
            {
                CheckBoxesVisible = true;
                var previewGame = GameHandler.GetMultiGameEditObject(Games);
                DataContext = previewGame;
                Game = previewGame;
                TabActions.Visibility = Visibility.Hidden;
                TabLinks.Visibility = Visibility.Hidden;
                TabInstallation.Visibility = Visibility.Hidden;
                ButtonDownload.Visibility = Visibility.Hidden;
            }
            else
            {
                DataContext = Game;

                if (Game.PlayTask != null)
                {
                    TempPlayTask = Playnite.CloneObject.CloneJson<GameTask>(Game.PlayTask);
                }

                if (Game.OtherTasks != null)
                {
                    TempOtherTasks = Playnite.CloneObject.CloneJson<ObservableCollection<GameTask>>(Game.OtherTasks);
                    OtherTasksItems.ItemsSource = TempOtherTasks;
                }

                if (Game.Links != null)
                {
                    TempLinks = Playnite.CloneObject.CloneJson<ObservableCollection<Link>>(Game.Links);
                }

                if (Game.Provider != Provider.Custom)
                {
                    TabInstallation.Visibility = Visibility.Collapsed;
                }

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ShowAddPlayAction"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ShowRemovePlayAction"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ShowPlayActionEdit"));
            }
        }

        private void PreviewGameData(IGame game)
        {
            var listConverter = new ListToStringConverter();
            var dateConverter = new NullableDateToStringConverter();

            TextName.Text = string.IsNullOrEmpty(game.Name) ? TextName.Text : game.Name;

            if (game.Developers != null && game.Developers.Count != 0)
            {
                TextDeveloper.Text = (string)listConverter.Convert(game.Developers, typeof(string), null, null);
            }

            if (game.Publishers != null && game.Publishers.Count != 0)
            {
                TextPublisher.Text = (string)listConverter.Convert(game.Publishers, typeof(string), null, null);
            }

            if (game.Genres != null && game.Genres.Count != 0)
            {
                TextGenres.Text = (string)listConverter.Convert(game.Genres, typeof(string), null, null);
            }

            TextReleaseDate.Text = (string)dateConverter.Convert(game.ReleaseDate, typeof(DateTime?), null, null);
            TextDescription.Text = string.IsNullOrEmpty(game.Description) ? TextName.Text : game.Description;

            if (game.Links != null)
            {
                TempLinks = game.Links;
                CheckLinks.IsChecked = true;
            }

            if (!string.IsNullOrEmpty(game.Image))
            {
                var extension = Path.GetExtension(game.Image);
                var tempPath = Path.Combine(Paths.TempPath, "tempimage" + extension);
                FileSystem.PrepareSaveFile(tempPath);
                Web.DownloadFile(game.Image, tempPath);
                ImageImage.Source = BitmapExtensions.BitmapFromFile(tempPath);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ImageImage.Tag.ToString()));
            }
        }

        private string SaveFileIconToTemp(string exePath)
        {
            var ico = IconExtension.ExtractIconFromExe(exePath, true);
            if (ico == null)
            {
                return string.Empty;
            }

            var tempPath = Path.Combine(Paths.TempPath, "tempico.png");
            if (ico != null)
            {
                FileSystem.PrepareSaveFile(tempPath);
                ico.ToBitmap().Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);
            }

            return tempPath;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ButtonOK_Click(object sender, RoutedEventArgs e)
        {
            if (Games == null)
            {
                if (string.IsNullOrWhiteSpace(TextName.Text))
                {
                    PlayniteMessageBox.Show("Name cannot be empty.", "Invalid game data", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            if (!string.IsNullOrEmpty(TextReleaseDate.Text) && !DateTime.TryParseExact(TextReleaseDate.Text, Playnite.Constants.DateUiFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                PlayniteMessageBox.Show("Release date in is not valid format.", "Invalid game data", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (IsNameBindingDirty && CheckName.IsChecked == true)
            {
                BindingOperations.GetBindingExpression(TextName, TextBox.TextProperty).UpdateSource();
                if (Games != null)
                {
                    foreach (var game in Games)
                    {
                        game.Name = Game.Name;
                    }
                }
            }
            
            if (IsGenreBindingDirty && CheckGenres.IsChecked == true)
            {
                BindingOperations.GetBindingExpression(TextGenres, TextBox.TextProperty).UpdateSource();
                if (Games != null)
                {
                    foreach (var game in Games)
                    {
                        game.Genres = Game.Genres;
                    }
                }
            }
            
            if (IsReleaseDateBindingDirty && CheckReleaseDate.IsChecked == true)
            {
                BindingOperations.GetBindingExpression(TextReleaseDate, TextBox.TextProperty).UpdateSource();
                if (Games != null)
                {
                    foreach (var game in Games)
                    {
                        game.ReleaseDate = Game.ReleaseDate;
                    }
                }
            }
            
            if (IsDeveloperBindingDirty && CheckDeveloper.IsChecked == true)
            {
                BindingOperations.GetBindingExpression(TextDeveloper, TextBox.TextProperty).UpdateSource();
                if (Games != null)
                {
                    foreach (var game in Games)
                    {
                        game.Developers = Game.Developers;
                    }
                }
            }

            if (IsPublisherBindingDirty && CheckPublisher.IsChecked == true)
            {
                BindingOperations.GetBindingExpression(TextPublisher, TextBox.TextProperty).UpdateSource();
                if (Games != null)
                {
                    foreach (var game in Games)
                    {
                        game.Publishers = Game.Publishers;
                    }
                }
            }
            
            if (IsCategoriesBindingDirty && CheckCategories.IsChecked == true)
            {
                BindingOperations.GetBindingExpression(TextCategories, TextBox.TextProperty).UpdateSource();
                if (Games != null)
                {
                    foreach (var game in Games)
                    {
                        game.Categories = Game.Categories;
                    }
                }
            }            

            if (IsDescriptionBindingDirty && CheckDescription.IsChecked == true)
            {
                BindingOperations.GetBindingExpression(TextDescription, TextBox.TextProperty).UpdateSource();
                if (Games != null)
                {
                    foreach (var game in Games)
                    {
                        game.Description = Game.Description;
                    }
                }
            }

            if (IsInstallDirBindingDirty)
            {
                BindingOperations.GetBindingExpression(TextInstallDir, TextBox.TextProperty).UpdateSource();
            }

            if (IsIsoPathBindingDirty)
            {
                BindingOperations.GetBindingExpression(TextIso, TextBox.TextProperty).UpdateSource();
            }

            if (IsPlatformBindingDirty && CheckPlatform.IsChecked == true)
            {
                BindingOperations.GetBindingExpression(ComboPlatforms, ComboBox.SelectedValueProperty).UpdateSource();
                if (Games != null)
                {
                    foreach (var game in Games)
                    {
                        game.PlatformId = Game.PlatformId;
                    }
                }
            }

            if (IsIconBindingDirty && CheckIcon.IsChecked == true)
            {
                var iconPath = ((BitmapImage)ImageIcon.Source).UriSource.OriginalString;
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(iconPath);
                var iconId = "images/custom/" + fileName;
                GameDatabase.Instance.AddImage(iconId, fileName, File.ReadAllBytes(iconPath));

                if (Games != null)
                {
                    foreach (var game in Games)
                    {
                        if (!string.IsNullOrEmpty(game.Icon))
                        {
                            GameDatabase.Instance.DeleteImageSafe(game.Icon, game);
                        }

                        game.Icon = iconId;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(Game.Icon))
                    {
                        GameDatabase.Instance.DeleteImageSafe(Game.Icon, Game);
                    }

                    Game.Icon = iconId;
                }

                if (Path.GetDirectoryName(iconPath) == Paths.TempPath)
                {
                    File.Delete(iconPath);
                }
            }

            if (IsImageBindingDirty && CheckImage.IsChecked == true)
            {
                var imagePath = ((BitmapImage)ImageImage.Source).UriSource.OriginalString;
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(imagePath);
                var imageId = "images/custom/" + fileName;
                GameDatabase.Instance.AddImage(imageId, fileName, File.ReadAllBytes(imagePath));

                if (Games != null)
                {
                    foreach (var game in Games)
                    {
                        if (!string.IsNullOrEmpty(game.Image))
                        {
                            GameDatabase.Instance.DeleteImageSafe(game.Image, game);
                        }

                        game.Image = imageId;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(Game.Image))
                    {
                        GameDatabase.Instance.DeleteImageSafe(Game.Image, Game);
                    }

                    Game.Image = imageId;
                }

                if (Path.GetDirectoryName(imagePath) == Paths.TempPath)
                {
                    File.Delete(imagePath);
                }
            }

            if (Games == null)
            {
                if (!Game.PlayTask.IsEqualJson(TempPlayTask))
                {
                    Game.PlayTask = TempPlayTask;
                }

                if (!Game.OtherTasks.IsEqualJson(TempOtherTasks))
                {
                    Game.OtherTasks = TempOtherTasks;
                }

                if (!Game.Links.IsEqualJson(TempLinks) && CheckLinks.IsChecked == true)
                {                    
                    Game.Links = TempLinks;
                }
            }

            if (Games != null)
            {
                foreach (var game in Games)
                {
                    GameDatabase.Instance.UpdateGameInDatabase(game);
                }
            }
            else
            {
                GameDatabase.Instance.UpdateGameInDatabase(Game);
            }

            DialogResult = true;
            Close();
        }

        private void ButtonSelectIcon_Click(object sender, RoutedEventArgs e)
        {
            var path = Dialogs.SelectIconFile(this);
            if (!string.IsNullOrEmpty(path))
            {
                if (path.EndsWith("exe", StringComparison.CurrentCultureIgnoreCase))
                {
                    path = SaveFileIconToTemp(path);

                    if (string.IsNullOrEmpty(path))
                    {
                        return;
                    }
                }               

                ImageIcon.Source = BitmapExtensions.BitmapFromFile(path);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ImageIcon.Tag.ToString()));
            }
        }

        private void ButtonExecIcon_Click(object sender, RoutedEventArgs e)
        {
            if (TempPlayTask == null || TempPlayTask.Type == GameTaskType.URL)
            {
                PlayniteMessageBox.Show(FindResource("ExecIconMissingPlayAction") as string);
                return;
            }

            var path = (Game as Game).ResolveVariables(TempPlayTask.Path);
            var icon = SaveFileIconToTemp(path);
            if (string.IsNullOrEmpty(icon))
            {
                return;
            }

            ImageIcon.Source = BitmapExtensions.BitmapFromFile(icon);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ImageIcon.Tag.ToString()));
        }

        private void ButtonSelectImage_Click(object sender, RoutedEventArgs e)
        {
            var path = Dialogs.SelectImageFile(this);
            if (!string.IsNullOrEmpty(path))
            {
                ImageImage.Source = BitmapExtensions.BitmapFromFile(path);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ImageImage.Tag.ToString()));
            }
        }

        private void ButtonAddPlayAction_Click(object sender, RoutedEventArgs e)
        {
            TempPlayTask = new GameTask()
            {
                Name = "Play",
                IsBuiltIn = false
            };
        }

        private void ButtonRemovePlayAction_Click(object sender, RoutedEventArgs e)
        {
            TempPlayTask = null;
        }

        private void ButtonAddAction_Click(object sender, RoutedEventArgs e)
        {
            if (TempOtherTasks == null)
            {
                TempOtherTasks = new ObservableCollection<GameTask>();
                OtherTasksItems.ItemsSource = TempOtherTasks;
            }

            var newTask = new GameTask()
            {
                Name = "New Action",
                IsBuiltIn = false
            };

            if (TempPlayTask != null && TempPlayTask.Type == GameTaskType.File)
            {
                newTask.WorkingDir = TempPlayTask.WorkingDir;
                newTask.Path = TempPlayTask.Path;
            }

            TempOtherTasks.Add(newTask);
        }

        private void ButtonDeleteAction_Click(object sender, RoutedEventArgs e)
        {
            var task = ((Button)sender).DataContext as GameTask;
            TempOtherTasks.Remove(task);
        }

        private void ButtonDownload_Click(object sender, RoutedEventArgs e)
        {
            ButtonDownload.ContextMenu.IsEnabled = true;
            ButtonDownload.ContextMenu.Width = ButtonDownload.ActualWidth;
            ButtonDownload.ContextMenu.PlacementTarget = ButtonDownload;
            ButtonDownload.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Top;
            ButtonDownload.ContextMenu.IsOpen = true;         
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var obj = (FrameworkElement)sender;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(obj.Tag.ToString()));
        }

        private void Image_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            var obj = (FrameworkElement)sender;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(obj.Tag.ToString()));
        }

        private void CheckIcon_Loaded(object sender, RoutedEventArgs e)
        {
            CheckIcon = sender as CheckBox;
        }

        private void CheckImage_Loaded(object sender, RoutedEventArgs e)
        {
            CheckImage = sender as CheckBox;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            positionManager.RestoreSizeAndLocation();
            var platforms = GameDatabase.Instance.PlatformsCollection.FindAll().OrderBy(a => a.Name).ToList();
            platforms.Insert(0, new Platform(string.Empty));
            ComboPlatforms.ItemsSource = platforms;
        }

        private void ButtonPickCat_Click(object sender, RoutedEventArgs e)
        {
            var converter = new ListToStringConverter();
            var dummyGame = new Game()
            {
                Categories = (ComparableList<string>)converter.ConvertBack(TextCategories.Text, typeof(ComparableList<string>), null, CultureInfo.InvariantCulture)
            };

            var window = new CategoryConfigWindow()
            {
                AutoUpdateGame = false,
                Game = dummyGame,
                Owner = this
            };

            window.ShowDialog();

            if (window.DialogResult == true)
            {
                TextCategories.Text = (string)converter.Convert(dummyGame.Categories, typeof(string), null, CultureInfo.InvariantCulture);
            }
        }

        private void DoMetadataLookup(MetadataProvider provider)
        {
            if (string.IsNullOrEmpty(TextName.Text))
            {
                PlayniteMessageBox.Show("Game name cannot be empty.", "", MessageBoxButton.OK);
                return;
            }

            var window = new MetadataLookupWindow()
            {
                DataContext = Game,
                ShowInTaskbar = false,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (window.LookupData(TextName.Text, provider) == true)
            {
                PreviewGameData(window.MetadataData);
                CheckBoxesVisible = true;
            }
        }

        private void MenuWikiDownload_Click(object sender, RoutedEventArgs e)
        {
            DoMetadataLookup(MetadataProvider.Wiki);
        }

        private void MenuIGDBDownload_Click(object sender, RoutedEventArgs e)
        {
            DoMetadataLookup(MetadataProvider.IGDB);
        }

        private void MenuStoreDownload_Click(object sender, RoutedEventArgs e)
        {
            TabControlMain.IsEnabled = false;
            TabControlMain.Opacity = 0.5;
            GridDownload.Visibility = Visibility.Visible;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    GameMetadata metadata;
                    var tempGame = (game as Game).CloneJson();
                    tempGame.Image = string.Empty;

                    switch (tempGame.Provider)
                    {
                        case Provider.Steam:
                            metadata = (new SteamLibrary()).UpdateGameWithMetadata(tempGame);
                            break;
                        case Provider.GOG:
                            metadata = (new GogLibrary()).UpdateGameWithMetadata(tempGame);
                            break;
                        case Provider.Origin:
                            metadata = (new OriginLibrary()).UpdateGameWithMetadata(tempGame);
                            break;
                        case Provider.Uplay:
                            return;
                        case Provider.Custom:
                        default:
                            return;
                    }

                    Dispatcher.Invoke(() =>
                    {
                        PreviewGameData(tempGame);
                        CheckBoxesVisible = true;
                    });
                }
                catch (Exception exc)
                {
                    logger.Error(exc, "Failed to download metadata, {0} , {1}", game.Provider, game.ProviderId);
                    PlayniteMessageBox.Show("Failed to download metadata: " + exc.Message, "Download Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    Dispatcher.Invoke(() =>
                    {
                        TabControlMain.IsEnabled = true;
                        TabControlMain.Opacity = 1;
                        GridDownload.Visibility = Visibility.Hidden;
                    });
                }
            });
        }

        private void ButtonRemoveLink_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var link = button.DataContext as Link;
            TempLinks.Remove(link);
        }

        private void ButtonAddLink_Click(object sender, RoutedEventArgs e)
        {
            if (TempLinks == null)
            {
                TempLinks = new ObservableCollection<Link>();
            }

            TempLinks.Add(new Link("NewLink", "NewUrl"));
        }

        private void ButtonMoveUpAction_Click(object sender, RoutedEventArgs e)
        {
            var task = (sender as Button).DataContext as GameTask;
            var index = TempOtherTasks.IndexOf(task);

            if (index != 0)
            {
                TempOtherTasks.Move(index, index - 1);
            }
        }

        private void ButtonMoveDownAction_Click(object sender, RoutedEventArgs e)
        {
            var task = (sender as Button).DataContext as GameTask;
            var index = TempOtherTasks.IndexOf(task);

            if (index != TempOtherTasks.Count - 1)
            {
                TempOtherTasks.Move(index, index + 1);
            }
        }

        private void ButtonBrowseInstallDir_Click(object sender, RoutedEventArgs e)
        {
            var path = Dialogs.SelectFolder(this);
            if (!string.IsNullOrEmpty(path))
            {
                TextInstallDir.Text = path;
            }
        }

        private void ButtonBrowseIso_Click(object sender, RoutedEventArgs e)
        {
            var path = Dialogs.SelectFile("*.*|*.*");
            if (!string.IsNullOrEmpty(path))
            {
                TextIso.Text = path;
            }
        }

        private void ComboPlatforms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var obj = (FrameworkElement)sender;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(obj.Tag.ToString()));
            if (ComboPlatforms.SelectedValue == null)
            {
                return;
            }

            var platformId = (int)ComboPlatforms.SelectedValue;
            if (platformId == 0)
            {
                Emulators = GameDatabase.Instance.EmulatorsCollection.FindAll().ToList();
            }
            else
            {
                Emulators = GameDatabase.Instance.EmulatorsCollection.FindAll().Where(a => a.Platforms != null && a.Platforms.Contains(platformId)).ToList();
            }
        }
    }
}
