### Nuget包管理器可以添加我的---程序包源---实现采用nuget包直接安装应用
名称：随意填-我以私有化代替
源(s):https://nuget.lingyanspace.com/v3/index.json
### 使用方法
//前期需自行判断是否需要升级，以及升级压缩包地址获取
//配置参数
LingYanAutoUpdateManager.Setting("网络升级压缩包", "升级完成后重启应用","版本文件存放位置","当前版本", "升级版本");
//开始启动升级
LingYanAutoUpdateManager.ToRun();
//扩展方法----自动获取=》升级完成后重启应用
LingYanAutoUpdateManager.GetRestartApp()
