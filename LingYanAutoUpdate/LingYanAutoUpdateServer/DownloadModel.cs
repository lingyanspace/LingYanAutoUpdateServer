namespace LingYanAutoUpdateServer
{
    public class DownloadModel: BasePropertyChanged
    {
		private double _CuurentProgress;

		public double CuurentProgress
        {
			get { return _CuurentProgress; }
			set { _CuurentProgress = value; this.OnPropertyChanged(); }
		}
		private double _HasDownloadValue;

		public double HasDownloadValue
        {
			get { return _HasDownloadValue; }
			set { _HasDownloadValue = value;this.OnPropertyChanged(); }
		}
		private double _TotalDownloadValue;

		public double TotalDownloadValue
        {
			get { return _TotalDownloadValue; }
			set { _TotalDownloadValue = value;this.OnPropertyChanged(); }
		}
		private double _DownloadSpeed;

		public double DownloadSpeed
        {
			get { return _DownloadSpeed; }
			set { _DownloadSpeed = value; this.OnPropertyChanged(); }
        }

	}
}
