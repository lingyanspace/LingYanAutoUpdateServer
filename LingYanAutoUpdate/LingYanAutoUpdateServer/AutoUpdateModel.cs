namespace LingYanAutoUpdateServer
{
    public class AutoUpdateModel
    {
        public string TitleName { get; set; }
        //升级包位置
        public string UpdatePackageZipUrl { get; set; }
        //升级完成后启动应用 
        public string RestartApp { get; set; }
        //存放版本文件
        public string LocalVersionDir { get; set; }
        //当前安装版本
        public string LocalVersion { get; set; }
        //线上最新版本
        public string ServerVersion { get; set; }
    }
}
