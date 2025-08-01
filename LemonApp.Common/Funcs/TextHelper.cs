﻿using LemonApp.MusicLib.Abstraction.Entities;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LemonApp.Common.Funcs;

public static class TextHelper
{
    /// <summary>
    /// 将数字转换为 "xx.x万",输入为0时返回null
    /// </summary>
    /// <param name="num"></param>
    /// <returns></returns>
    public static string? IntToWn(this int num)
    {
        if (num == 0) return null;
        if (num < 10000)
            return num.ToString();
        else
        {
            double d = (double)num / (double)10000;
            return Math.Round(d, 2, MidpointRounding.AwayFromZero) + "万";
        }
    }
    /// <summary>
    /// 过滤掉路径文件名中的非法字符
    /// </summary>
    /// <param name="text"></param>
    /// <param name="replacement">替换的字符</param>
    /// <returns></returns>
    public static string MakeValidFileName(string text, string replacement = "_")
    {
        StringBuilder str = new StringBuilder();
        var invalidFileNameChars = Path.GetInvalidFileNameChars();
        foreach (var c in text)
        {
            if (invalidFileNameChars.Contains(c))
                str.Append(replacement ?? "");
            else
                str.Append(c);
        }

        return str.ToString();
    }
    /// <summary>
    /// 去除emoji表情信息
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string Exem(string str)
    {
        string s = str;
        while (s.Contains("[em]"))
        {
            string em = "[em]" + FindTextByAB(s, "[em]", "[/em]", 0) + "[/em]";
            s = s.Replace(em, "");
        }
        return s;
    }

    /// <summary>
    /// 查找中间文本
    /// </summary>
    /// <param name="all"></param>
    /// <param name="r">前面的文本</param>
    /// <param name="l">后面的文本</param>
    /// <param name="t"></param>
    /// <returns></returns>
    public static string? FindTextByAB(string all, string r, string l, int t)
    {

        int A = all.IndexOf(r, t);
        int B = all.IndexOf(l, A + 1);
        if (A < 0 || B < 0)
        {
            return null;
        }
        else
        {
            A += r.Length;
            B -= A;
            if (A < 0 || B < 0)
            {
                return null;
            }
            return all.Substring(A, B);
        }
    }

    public static bool FuzzySearch(Music m,string key)
    {
        if (m == null || string.IsNullOrWhiteSpace(key)) return false;

        key= key.ToLower();
        string content = $"{m.MusicName} {m.SingerText} {m.Album?.Name ?? ""}".ToLower();
        return content.Contains(key);
    }
    public static string MD5Hash(string text)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(text);
        byte[] hash = MD5.HashData(inputBytes);
        var sb = new StringBuilder();
        foreach (byte b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
