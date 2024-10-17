﻿using System;

namespace LemonApp.Services;
public enum PageType
{
    SettingsPage
}
public class MainNavigationService
{
    public event Action<PageType,object?>? OnNavigatingRequsted;

    public void RequstNavigation<T>(PageType type,T? arg) where T:class
    {
        OnNavigatingRequsted?.Invoke(type, arg);
    }
    public void RequstNavigation(PageType type)
    {
        OnNavigatingRequsted?.Invoke(type,null);
    }
}
