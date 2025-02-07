using Prolizy.Viewer.Controls.Edt;

namespace Prolizy.Viewer.Utilities.Android;

public interface IAndroidAccess
{
    
    public void UpdateWidget(ScheduleItem currentItem, bool isCurrent);
    
    public void OpenFolder(string path);
    
}

public static class AndroidAccessManager
{
    public static IAndroidAccess? AndroidAccess { get; set; }
}