using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace LubanSupport.Editor
{
    /// <summary>
    /// Excel 配表快速搜索弹窗
    /// </summary>
    public class QuickSearchDataSourceDropdown : AdvancedDropdown
    {
        private readonly string[] _dataList;

        public QuickSearchDataSourceDropdown(AdvancedDropdownState state) : base(state)
        {
            _dataList = new FileInfo(LubanTool.Config.ConfigPath).Directory!
                .GetFiles("*.xlsx")
                .Where(x => !x.Name.StartsWith('~'))
                .Select(x => x.Name)
                .ToArray();
            minimumSize = new Vector2(200, 400);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Excels");
            foreach (var dataStr in _dataList)
            {
                root.AddChild(new AdvancedDropdownItem(dataStr) { id = dataStr.GetHashCode() });
            }
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);

            var directoryInfo = new FileInfo(LubanTool.Config.ConfigPath).Directory;
            if (directoryInfo == null) return;

            var file = new FileInfo(Path.Combine(directoryInfo.FullName, item.name));
            if (!file.Exists) return;

            try
            {
                Process.Start("Excel.exe", file.FullName);
            }
            catch (Win32Exception)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = file.FullName,
                    UseShellExecute = true
                });
            }
        }
    }
}
