using CommunityToolkit.Mvvm.Input;
using DeviceEmulator.ViewModel;
using LogProfiler.Models;
using LogProfiler.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace LogProfiler.ViewModels
{
    public class RegexMessageModel : NotifyBase
    {
        private RegexData? _SelectedItem;
        public RegexData? SelectedItem
        {
            get => _SelectedItem;
            set
            {
                _SelectedItem = value;
                MyPropertyChanged();
                if (SelectedItem == null)
                {
                    DeleteButtonName = "清空";
                }
                else
                {
                    DeleteButtonName = "删除";
                }
            }
        }

        private string? _DeleteButtonName = "清空";
        public string? DeleteButtonName
        {
            get => _DeleteButtonName;
            set { _DeleteButtonName = value; MyPropertyChanged(); }
        }

        private string? _TextBlockText;
        public string? TextBlockText
        {
            get => _TextBlockText;
            set { _TextBlockText = value; MyPropertyChanged(); }
        }

        public EventHandler<string>? RegexMessageEventHandler;
        public ObservableCollection<RegexData> RegexMessages { get; set; } = new ObservableCollection<RegexData>();

        private RelayCommand? _AddRegexCmd;
        public RelayCommand? AddRegexCmd
        {
            get => _AddRegexCmd ?? new RelayCommand(AddRegexCmdExd);
            set => _AddRegexCmd = value;
        }

        private void AddRegexCmdExd()
        {
            RegexData regexData = new RegexData { RegetName = $"正则表达式{RegexMessages.Count + 1}" };
            RegexMessages.Add(regexData);
            regexData.PropertyChanged += RegexDataEventHandler;
        }

        /// <summary>
        /// 当勾选多个正则表达式时，开启联合查询
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RegexDataEventHandler(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsChecked")
            {
                if (RegexMessages.Where(regex => regex.IsChecked == true).Count() > 1)
                {
                    TextBlockText = "使用联合查询";
                }
                else
                {
                    TextBlockText = string.Empty;
                }
                
            }
        }

        private void RegexMessages_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if ( RegexMessages.Count < 2)
            {
                TextBlockText = string.Empty;
            }
        }

        private RelayCommand? _DeleteRegexCmd;
        public RelayCommand? DeleteRegexCmd
        {
            get => _DeleteRegexCmd ?? new RelayCommand(DeleteRegexCmdExd);
            set => _DeleteRegexCmd = value;
        }

        private void DeleteRegexCmdExd()
        {
            if (SelectedItem != null)
            {
                RegexMessages.Remove(SelectedItem);
                SelectedItem = null;
            }
            else
            {
                RegexMessages.Clear();
            }
        }

        private RelayCommand? _SaveRegexCmd;
        public RelayCommand? SaveRegexCmd
        {
            get => _SaveRegexCmd ?? new RelayCommand(SaveRegexCmdExd);
            set => _SaveRegexCmd = value;
        }

        private void SaveRegexCmdExd()
        {
            string regexStr = string.Join(',', RegexMessages.Select(regex => { return regex.IsChecked == true ? regex.RegexStr: string.Empty; }));
            RegexMessageEventHandler?.Invoke(this, regexStr);
            var window = Application.Current.Windows.OfType<RegexMessageView>().FirstOrDefault();
            window?.Close();
        }

        public RegexMessageModel()
        {
            RegexMessages.CollectionChanged += RegexMessages_CollectionChanged;
        }
    }
}
