namespace LingYanAutoUpdateServer
{
    public class ExtractModel: BasePropertyChanged
    {
        private double _CurrentExtractProgress;

        public double CurrentExtractProgress
        {
            get { return _CurrentExtractProgress; }
            set { _CurrentExtractProgress = value; this.OnPropertyChanged(); }
        }
        private string _CurrentFileName;

        public string CurrentFileName
        {
            get { return _CurrentFileName; }
            set { _CurrentFileName = value; this.OnPropertyChanged(); }
        }
        private int _TotalFiles;

        public int TotalFiles
        {
            get { return _TotalFiles; }
            set { _TotalFiles = value; this.OnPropertyChanged(); }
        }
        private int _CurrentFileIndex;

        public int CurrentFileIndex
        {
            get { return _CurrentFileIndex; }
            set { _CurrentFileIndex = value; this.OnPropertyChanged(); }
        }
    }
}
