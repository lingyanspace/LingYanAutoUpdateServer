### Nuget包管理器可以添加我的---程序包源---实现采用nuget包直接安装应用

```
名称：随意填
源(s):https://nuget.lingyanspace.com/v3/index.json
```

### 使用方法

```
针对WPF软件设计的自动升级程序,全局只需配置一次即可，需要升级时ToRun()就行
使用极简步骤1：
//参数1：窗体标题
//参数2：你的http或https网络升级压缩包
//参数3：存放本次升级后最新版本的文件
//参数4：你的本地版本
//参数5：你的更新版本
   LingYanAutoUpdateManager.Setting("测试升级",updateURL, "my.txt", "1.0", "2.0");
使用极简步骤2：
//需要升级时直接运行即可
   LingYanAutoUpdateManager.ToRun();
```

