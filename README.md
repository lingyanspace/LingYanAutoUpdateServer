### Nuget包管理器可以添加我的---程序包源---实现采用nuget包直接安装应用

```
名称：随意填
源(s):> https://nuget.lingyanspace.com/v3/index.json

```

### 使用方法

```
针对WPF软件设计的自动升级程序,全局只需配置一次即可，需要升级时ToRun()就行
使用极简步骤1：
//参数1：窗体标题
//参数2：你的http或https网络升级压缩包
//参数3：存放本次升级后最新版本号的文件,如果只填写文件则默认在当前目录下，如有必要可以直接指定文件路径
//参数4：你的本地版本
//参数5：你的更新版本
   >  LingYanAutoUpdateManager.Setting("测试升级",updateURL, "my.txt", "1.0", "2.0");
使用极简步骤2：
//需要升级时直接运行即可
   > LingYanAutoUpdateManager.ToRun(); 
```

```
注意事项
> 如果是.net framework系列的则直接安装包即可使用；
> 如果是.net core系列则需要在安装包之后将虚拟的UpdateAppFloder文件右键找到文件夹位置，直接复制过来即可。
```
![输入图片说明](%E4%BC%81%E4%B8%9A%E5%BE%AE%E4%BF%A1%E6%88%AA%E5%9B%BE_17373589821558.png)
