using Microsoft.Playwright;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

class Program
{
    private const string RunRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    static async Task Main()
    {
        //// 检查状态
        //bool isAutoStart = IsInStartup();
        //if (isAutoStart)
        //{
        //    // 移除自启动
        //    //RemoveFromStartup();
        //}
        //else
        //{
        //    // 添加自启动
        //    if (AddToStartup())
        //    {
        //        Console.WriteLine("已添加开机自启动");
        //    }
        //}

        while (true)
        {
            if (CanVisitNetworkAsync().Result)
            {
                break;
            }
            Thread.Sleep(1000);
        }

        // 创建Playwright实例
        var playwright = await Playwright.CreateAsync();

        // 指定用户数据目录路径
        string userDataDir = @"C:\JDPriceProtection";

        // 启动浏览器并使用持久化上下文
        var browser = await playwright.Chromium.LaunchPersistentContextAsync(userDataDir, new BrowserTypeLaunchPersistentContextOptions
        {
            Headless = false // 设置为false可以看到浏览器界面
        });

        // 创建新页面
        var page = await browser.NewPageAsync();

        try
        {
            // 监听 JavaScript 对话框事件
            page.Dialog += async (sender, dialog) =>
            {
                if (dialog.Type == DialogType.Confirm)
                {
                    // 这里选择接受确认框（点击"确定"）
                    // await dialog.AcceptAsync();
                    // 如果要点"取消"，用 await dialog.DismissAsync();
                }
                else
                {
                    await dialog.DismissAsync();
                }
            };

            // 导航到指定URL
            await page.GotoAsync("https://msitepp-fm.jd.com/rest/priceprophone/priceProPhoneMenu");

            // 等待页面加载完成
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // 检查是否存在id为one-btn的按钮
            var canClickButton = await page.Locator("#one-btn").IsVisibleAsync();
            var canNotClickButton = await page.Locator("#one-btn-dis").IsVisibleAsync();

            if (canClickButton)
            {
                // 点击按钮
                await page.ClickAsync("#one-btn");
                // 等待一段时间以确保内容加载完成（可选）
                await page.WaitForTimeoutAsync(2000);
                // 检查页面是否包含特定文字
                bool text1 = await page.QuerySelectorAsync("text='10分钟内只能申请一次'") != null;
                bool text2 = await page.QuerySelectorAsync("text='单日申请上限10次'") != null;
                bool text3 = await page.QuerySelectorAsync("text='您购买的时候已是较划算的价格，当前无差价'") != null;
                bool text4 = await page.QuerySelectorAsync("text='恭喜您价保成功'") != null;
                if (text4)
                {
                    Console.ReadLine();
                }
            }
            else
            {
                if (canNotClickButton)
                {
                }
                else
                {
                    // 执行 JavaScript 触发 confirm 对话框
                    await page.EvaluateAsync(@"() => {
                        confirm('需要登录京东账户，请先登录，登录完成后请关闭浏览器，后续不用重复登录。');
                    }");
                    Console.ReadLine();
                }
            }
        }
        catch (Exception)
        {
        }
        finally
        {
            //关闭浏览器
            await browser.CloseAsync();
            //如果不是调试模式，则关闭控制台窗口
            Environment.Exit(0);
        }
    }

    private static async Task<bool> CanVisitNetworkAsync()
    {
        using (HttpClient client = new HttpClient())
        {
            try
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                HttpResponseMessage response = await client.GetAsync("https://www.baidu.com");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("网络正常");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"网络异常：{ex.Message}");
            }
        }
        return false;
    }
    /// <summary>
    /// 添加程序到开机自启动
    /// </summary>
    /// <returns>操作是否成功</returns>
    public static bool AddToStartup()
    {
        try
        {
            string appPath = Application.ExecutablePath;  // 更可靠的方式获取程序路径
            string appName = Application.ProductName;

            // 使用CurrentUser作用域，避免需要管理员权限
            using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(RunRegistryPath, true))
            {
                if (runKey == null)
                {
                    // 如果项不存在则创建
                    using (RegistryKey newKey = Registry.CurrentUser.CreateSubKey(RunRegistryPath))
                    {
                        newKey.SetValue(appName, appPath);
                    }
                }
                else
                {
                    runKey.SetValue(appName, appPath);
                }
            }

            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // 权限不足
            return false;
        }
        catch (Exception ex)
        {
            // 记录日志或处理其他异常
            Console.WriteLine($"添加开机启动失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 从开机自启动中移除程序
    /// </summary>
    /// <returns>操作是否成功</returns>
    public static bool RemoveFromStartup()
    {
        try
        {
            string appName = Application.ProductName;

            using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(RunRegistryPath, true))
            {
                if (runKey != null && runKey.GetValue(appName) != null)
                {
                    runKey.DeleteValue(appName, false);
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"移除开机启动失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 检查程序是否已设置开机自启动
    /// </summary>
    /// <returns>是否已设置自启动</returns>
    public static bool IsInStartup()
    {
        try
        {
            string appName = Application.ProductName;
            string appPath = Application.ExecutablePath;

            using (RegistryKey runKey = Registry.CurrentUser.OpenSubKey(RunRegistryPath, false))
            {
                if (runKey != null)
                {
                    var value = runKey.GetValue(appName) as string;
                    // 比较路径，确保是同一个程序
                    return !string.IsNullOrEmpty(value) &&
                           string.Equals(value, appPath, StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
