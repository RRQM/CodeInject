// ------------------------------------------------------------------------------
// 此代码版权（除特别声明或在XREF结尾的命名空间的代码）归作者本人若汝棋茗所有
// 源代码使用协议遵循本仓库的开源协议及附加协议，若本仓库没有设置，则按MIT开源协议授权
// CSDN博客：https://blog.csdn.net/qq_40374647
// 哔哩哔哩视频：https://space.bilibili.com/94253567
// Gitee源代码仓库：https://gitee.com/RRQM_Home
// Github源代码仓库：https://github.com/RRQM
// API首页：https://touchsocket.net/
// 交流QQ群：234762506
// 感谢您的下载和使用
// ------------------------------------------------------------------------------

using System;

namespace CodeInject
{
    /// <summary>
    /// 用于指定要注入的文件名、区域名称和占位符的特性。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class RegionInjectAttribute : Attribute
    {
        /// <summary>
        /// 获取或设置要注入的文件名。如果为null或空字符串，则搜索所有可用文件。
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 获取或设置要注入的区域名称。
        /// </summary>
        public string RegionName { get; set; }

        /// <summary>
        /// 获取或设置用于替换的占位符数组。
        /// </summary>
        public string[] Placeholders { get; set; } = new string[0];
    }
}