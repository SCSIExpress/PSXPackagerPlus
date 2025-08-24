using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;

namespace PSXPackagerGUI.Models
{
    public class SingleModel : BaseNotifyModel
    {
        private IEnumerable<Disc> _discs;
        private bool _isDirty;
        private double _progress;
        private double _maxProgress;
        private Disc _selectedDisc;
        private ResourceModel _icon0;
        private ResourceModel _icon1;
        private ResourceModel _pic0;
        private ResourceModel _pic1;
        private ResourceModel _snd0;
        private string _status;
        private ScreenScraperModel _screenScraperModel;

        public SingleModel()
        {
            Discs = new List<Disc>()
            {
                new Disc()
                {
                    Title = "Final Fantasy VII",
                    Size = 123456789
                }
            };

            InitializeScreenScraperModel();
        }

        private void InitializeScreenScraperModel()
        {
            _screenScraperModel = new ScreenScraperModel();
            _screenScraperModel.GetCurrentDiscPath = () => SelectedDisc?.Path;
            _screenScraperModel.SetGameArt = SetGameArtFromBytes;
            _screenScraperModel.SetIcon0 = SetIcon0FromBytes;
        }
        
        public Disc SelectedDisc { get => _selectedDisc; set => SetProperty(ref _selectedDisc, value); }
        public IEnumerable<Disc> Discs { get => _discs; set => SetProperty(ref _discs, value); }

        public ResourceModel Icon0 { get => _icon0; set => SetProperty(ref _icon0, value); }
        public ResourceModel Icon1 { get => _icon1; set => SetProperty(ref _icon1, value); }
        public ResourceModel Pic0 { get => _pic0; set => SetProperty(ref _pic0, value); }
        public ResourceModel Pic1 { get => _pic1; set => SetProperty(ref _pic1, value); }
        public ResourceModel Snd0 { get => _snd0; set => SetProperty(ref _snd0, value); }

        public ScreenScraperModel ScreenScraperModel { get => _screenScraperModel; set => SetProperty(ref _screenScraperModel, value); }

        public string Status { get => _status; set => SetProperty(ref _status, value); }
        public double Progress { get => _progress; set => SetProperty(ref _progress, value); }
        public double MaxProgress { get => _maxProgress; set => SetProperty(ref _maxProgress, value); }

        public bool IsDirty { get => _isDirty; set => SetProperty(ref _isDirty, value); }
        public bool IsBusy { get; set; }

        public bool IsNew { get; set; }

        private void SetGameArtFromBytes(byte[] imageBytes, string extension)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(imageBytes);
                bitmap.EndInit();

                // Determine which resource to set based on image dimensions or user preference
                // For now, we'll set it as PIC0 (background image)
                if (Pic0 == null)
                    Pic0 = new ResourceModel();

                Pic0.Icon = bitmap;
                Pic0.IsEmpty = false;
                Pic0.IsLoadEnabled = true;
                Pic0.IsSaveAsEnabled = true;
                Pic0.IsRemoveEnabled = true;
                IsDirty = true;
            }
            catch (System.Exception ex)
            {
                // Handle image loading error
                Status = $"Failed to load image: {ex.Message}";
            }
        }

        private void SetIcon0FromBytes(byte[] imageBytes, string extension)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return;

            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(imageBytes);
                bitmap.EndInit();

                // Set as ICON0
                if (Icon0 == null)
                    Icon0 = new ResourceModel();

                Icon0.Icon = bitmap;
                Icon0.IsEmpty = false;
                Icon0.IsLoadEnabled = true;
                Icon0.IsSaveAsEnabled = true;
                Icon0.IsRemoveEnabled = true;
                IsDirty = true;
                Status = "ICON0 set from ScreenScraper";
            }
            catch (System.Exception ex)
            {
                // Handle image loading error
                Status = $"Failed to load ICON0: {ex.Message}";
            }
        }
    }
}