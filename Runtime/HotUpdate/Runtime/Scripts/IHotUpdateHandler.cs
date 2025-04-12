using System;
using System.Collections;
using System.Threading.Tasks;

public interface IHotUpdateHandler
{
    /// <summary>
    /// 检查资源更新
    /// </summary>
    /// <returns></returns>
    public IEnumerator CheckResources();

    /// <summary>
    /// 补充元数据
    /// </summary>
    /// <returns></returns>
    public IEnumerator LoadMetaData();

    /// <summary>
    /// 加载程序集
    /// </summary>
    /// <returns></returns>
    public IEnumerator LoadDll();
}