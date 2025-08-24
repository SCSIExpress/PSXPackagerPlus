﻿using System.Windows.Media;
using Popstation.Pbp;

namespace PSXPackagerGUI.Models
{
    public class ResourceModel : BaseNotifyModel
    {
        private ImageSource _icon;
        private string _text;
        private bool _isRemoveEnabled;
        private bool _isSaveAsEnabled;
        private bool _isLoadEnabled;
        private bool _isEmpty;

        public ResourceModel()
        {
            IsLoadEnabled = true;
            IsEmpty = true;
        }

        public void Reset()
        {
            Icon = null;
            IsLoadEnabled = true;
            IsSaveAsEnabled = false;
            IsRemoveEnabled = false;
            IsEmpty = true;
            SourceUrl = null;
        }

        public ResourceType Type { get; set; }

        public ImageSource Icon
        {
            get => _icon;
            set => SetProperty(ref _icon, value);
        }

        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        public bool IsLoadEnabled
        {
            get => _isLoadEnabled;
            set => SetProperty(ref _isLoadEnabled, value);
        }

        public bool IsSaveAsEnabled
        {
            get => _isSaveAsEnabled;
            set => SetProperty(ref _isSaveAsEnabled, value);
        }

        public bool IsRemoveEnabled
        {
            get => _isRemoveEnabled;
            set => SetProperty(ref _isRemoveEnabled, value);
        }

        public bool IsEmpty
        {
            get => _isEmpty;
            set => SetProperty(ref _isEmpty, value);
        }

        public string SourceUrl { get; set; }
    }
}