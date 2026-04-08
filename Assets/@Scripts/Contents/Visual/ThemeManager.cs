using UnityEngine;
using MB.Infrastructure.Messages;

namespace MB.Visual
{
    public class ThemeManager
    {
        public ColorThemeSO CurrentTheme { get; private set; }

        public void Init()
        {
            var defaultTheme = Resources.Load<ColorThemeSO>("Themes/Theme_Pastel");
            if (defaultTheme != null)
            {
                CurrentTheme = defaultTheme;
                GameLogger.Success("ThemeManager", $"기본 테마 로드: {defaultTheme.themeName}");
            }
            else
            {
                GameLogger.Warning("ThemeManager", "기본 테마를 찾을 수 없습니다. Resources/Themes/Theme_Pastel 확인");
            }
        }

        public void SetTheme(ColorThemeSO theme)
        {
            if (theme == null) return;
            CurrentTheme = theme;
            Managers.PublishAction(ActionId.Visual_ThemeChanged);
            GameLogger.Info("ThemeManager", $"테마 변경: {theme.themeName}");
        }
    }
}
