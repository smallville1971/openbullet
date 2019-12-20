﻿using Microsoft.Win32;
using OpenBullet.ViewModels;
using RuriLib.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace OpenBullet.Views.Main
{
    /// <summary>
    /// Logica di interazione per HitsDB.xaml
    /// </summary>
    public partial class HitsDB : Page
    {
        public HitsDBViewModel vm = new HitsDBViewModel();
        private GridViewColumnHeader listViewSortCol = null;
        private SortAdorner listViewSortAdorner = null;

        private IEnumerable<Hit> Selected => hitsListView.SelectedItems.Cast<Hit>();

        #region Mappings
        Func<Hit, string> mappingCapture = new Func<Hit, string>(hit => $"{hit.Data} | {hit.CapturedData}");

        Func<Hit, string> mappingFull = new Func<Hit, string>(hit =>
        {
            return "Data = " + hit.Data +
                    " | Type = " + hit.Type +
                    " | Config = " + hit.ConfigName +
                    " | Wordlist = " + hit.WordlistName +
                    " | Proxy = " + hit.Proxy +
                    " | Date = " + hit.Date.ToLongDateString() +
                    " | CapturedData = " + hit.CapturedData.ToCaptureString();
        });
        #endregion

        public HitsDB()
        {
            InitializeComponent();

            DataContext = vm;

            vm.RefreshList();

            var defaults = new string[] { "SUCCESS", "NONE" };
            foreach (string i in defaults.Concat(Globals.environment.GetCustomKeychainNames()))
                typeFilterCombobox.Items.Add(i);

            typeFilterCombobox.SelectedIndex = 0;

            configFilterCombobox.Items.Add(HitsDBViewModel.defaultFilter);
            foreach (string c in vm.ConfigsList.OrderBy(c => c))
                configFilterCombobox.Items.Add(c);

            configFilterCombobox.SelectedIndex = 0;

            var menu = (ContextMenu)Resources["ItemContextMenu"];
            var copyMenu = (MenuItem)menu.Items[0];
            var saveMenu = (MenuItem)menu.Items[1];
            foreach (var f in Globals.environment.ExportFormats)
            {
                MenuItem i = new MenuItem();
                i.Header = f.Format;
                i.Click += new RoutedEventHandler(copySelectedCustom_Click);
                ((MenuItem)copyMenu.Items[4]).Items.Add(i); // Here the 4 is hardcoded, it's bad but it works
            }

            foreach (var f in Globals.environment.ExportFormats)
            {
                MenuItem i = new MenuItem();
                i.Header = f.Format;
                i.Click += new RoutedEventHandler(saveSelectedCustom_Click);
                ((MenuItem)saveMenu.Items[3]).Items.Add(i); // Here the 3 is hardcoded, it's bad but it works
            }
        }

        public void AddConfigToFilter(string name)
        {
            if (!configFilterCombobox.Items.Cast<string>().Any(i => i == name))
            {
                configFilterCombobox.Items.Add(name);
            }
        }

        private void configFilterCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            vm.ConfigFilter = configFilterCombobox.SelectedValue.ToString();
            Globals.LogInfo(Components.HitsDB, $"Changed config filter to {vm.ConfigFilter}, found {vm.Total} hits");
        }

        private void typeFilterCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            vm.TypeFilter = (string)typeFilterCombobox.SelectedValue;
            Globals.LogInfo(Components.HitsDB, $"Changed type filter to {vm.TypeFilter}, found {vm.Total} hits");
        }

        private void purgeButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.LogWarning(Components.HitsDB, "Purge selected, prompting warning");

            if (MessageBox.Show("This will purge the WHOLE Hits DB, are you sure you want to continue?", "WARNING", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                Globals.LogInfo(Components.HitsDB, "Purge initiated");

                vm.RemoveAll();

                Globals.LogInfo(Components.HitsDB, "Purge finished");
            }
            else { Globals.LogInfo(Components.HitsDB, "Purge dismissed"); }
        }

        private void listViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader column = (sender as GridViewColumnHeader);
            string sortBy = column.Tag.ToString();
            if (listViewSortCol != null)
            {
                AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
                hitsListView.Items.SortDescriptions.Clear();
            }

            ListSortDirection newDir = ListSortDirection.Ascending;
            if (listViewSortCol == column && listViewSortAdorner.Direction == newDir)
                newDir = ListSortDirection.Descending;

            listViewSortCol = column;
            listViewSortAdorner = new SortAdorner(listViewSortCol, newDir);
            AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
            hitsListView.Items.SortDescriptions.Add(new SortDescription(sortBy, newDir));
        }

        private void ListViewItem_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        private string GetSaveFile()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "TXT files | *.txt";
            sfd.FilterIndex = 1;
            sfd.ShowDialog();
            return sfd.FileName;
        }

        #region Copy
        private void copySelectedData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Selected.CopyToClipboard(hit => hit.Data);
            }
            catch (Exception ex)
            {
                Globals.LogError(Components.HitsDB, $"Exception while copying hits - {ex.Message}");
            }
        }

        private void copySelectedCapture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Selected.CopyToClipboard(mappingCapture);
            }
            catch (Exception ex)
            {
                Globals.LogError(Components.HitsDB, $"Exception while copying hits - {ex.Message}");
            }
        }

        private void copySelectedFull_Click(object sender, RoutedEventArgs e)
        {
            try 
            {
                Selected.CopyToClipboard(mappingFull);
            }
            catch (Exception ex)
            { 
                Globals.LogError(Components.HitsDB, $"Exception while copying hits - {ex.Message}"); 
            }
        }

        private void copySelectedCustom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Selected.CopyToClipboard(hit => hit.ToFormattedString((sender as MenuItem).Header.ToString().Replace(@"\r\n", "\r\n").Replace(@"\n", "\n")));
            }
            catch (Exception ex)
            {
                Globals.LogError(Components.HitsDB, $"Exception while copying hits - {ex.Message}");
            }
        }
        #endregion

        #region Save
        private void saveSelectedData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Selected.SaveToFile(GetSaveFile(), hit => hit.Data);
            }
            catch (Exception ex) 
            { 
                Globals.LogError(Components.HitsDB, $"Exception while saving hits - {ex.Message}"); 
            }
        }

        private void saveSelectedCapture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Selected.SaveToFile(GetSaveFile(), mappingCapture);
            }
            catch (Exception ex)
            {
                Globals.LogError(Components.HitsDB, $"Exception while saving hits - {ex.Message}");
            }
        }

        private void saveSelectedFull_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Selected.SaveToFile(GetSaveFile(), mappingFull);
            }
            catch (Exception ex)
            {
                Globals.LogError(Components.HitsDB, $"Exception while saving hits - {ex.Message}");
            }
        }

        private void saveSelectedCustom_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Selected.SaveToFile(GetSaveFile(), hit => hit.ToFormattedString((sender as MenuItem).Header.ToString().Replace(@"\r\n", "\r\n").Replace(@"\n", "\n")));
            }
            catch (Exception ex)
            {
                Globals.LogError(Components.HitsDB, $"Exception while copying hits - {ex.Message}");
            }
        }
        #endregion

        private void selectAll_Click(object sender, RoutedEventArgs e)
        {
            hitsListView.SelectAll();
        }

        private void copySelectedProxy_Click(object sender, RoutedEventArgs e)
        {
            try 
            {
                var hit = (Hit)hitsListView.SelectedItem;
                Clipboard.SetText(hit.Proxy);    
            } 
            catch (Exception ex) 
            {
                Globals.LogError(Components.HitsDB, $"Failed to copy selected proxy - {ex.Message}"); 
            }
        }
        
        private void searchButton_Click(object sender, RoutedEventArgs e)
        {
            vm.SearchString = searchBar.Text;
            Globals.LogInfo(Components.HitsDB, "Changed capture filter to '"+ vm.SearchString + $"', found {vm.Total} hits");
        }

        private void sendToRecheck_Click(object sender, RoutedEventArgs e)
        {
            if (hitsListView.SelectedItems.Count == 0) { Globals.LogError(Components.HitsDB, "No hits selected!", true); return; }
            var first = (Hit)hitsListView.SelectedItem;
            var partialName = "Recheck-" + first.ConfigName;
            var wordlist = new Wordlist(partialName, "NULL", Globals.environment.RecognizeWordlistType(first.Data), "", true, true);

            var manager = Globals.mainWindow.RunnerManagerPage.vm;
            manager.CreateRunner();
            var runner = manager.Runners.Last().Page;
            Globals.mainWindow.ShowRunner(runner);

            runner.vm.SetWordlist(wordlist);
            runner.vm.DataPool = new DataPool (hitsListView.SelectedItems.Cast<Hit>().Select(h => h.Data).ToList());

            // Try to select the config referring to the first selected hit
            try
            {
                var cfg = Globals.mainWindow.ConfigsPage.ConfigManagerPage.vm.ConfigsList.First(c => c.Name == first.ConfigName).Config;
                runner.vm.SetConfig(cfg, false);
                runner.vm.BotsNumber = Math.Min(cfg.Settings.SuggestedBots, hitsListView.SelectedItems.Count);
            }
            catch { }

            // Switch to Runner
            Globals.mainWindow.menuOptionRunner_MouseDown(this, null);
        }

        private void deleteSelected_Click(object sender, RoutedEventArgs e)
        {
            Globals.LogInfo(Components.HitsDB, $"Deleting {hitsListView.SelectedItems.Count} hits");

            foreach (var hit in Selected.ToList())
            {
                vm.Remove(hit);
            }

            Globals.LogInfo(Components.HitsDB, "Succesfully sent the delete query and refreshed the list");
        }

        private void removeDuplicatesButton_Click(object sender, RoutedEventArgs e)
        {
            vm.DeleteDuplicates();
            Globals.LogInfo(Components.HitsDB, "Deleted duplicate hits");
        }

        private void deleteFilteredButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.LogWarning(Components.HitsDB, "Delete filtered selected, prompting warning");

            if (MessageBox.Show("This will delete all the hits that are currently being displayed, are you sure you want to continue?", "WARNING", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            vm.DeleteFiltered();

            Globals.LogInfo(Components.HitsDB, "Deleted filtered hits");
        }
    }
}