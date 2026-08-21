using System;
using System.Collections.Generic;
using NestSuite.Services;

namespace NestSuite.IdeaNest.Services;

/// <summary>カードタイトル・本文プレビュー内の1区間（通常 or 一致）。</summary>
public readonly record struct IdeaNestHighlightSegment(string Text, bool IsMatch);

/// <summary>
/// IdeaNestカード一覧のタイトル・本文プレビューで、検索語との一致箇所を
/// 表示用セグメントへ分割する。3-Run分割 helper
/// （<see cref="SearchMatchSegments.Split"/>）を繰り返し呼び出す薄いラッパーであり、
/// 同一の IndexOf 分割ロジックを複製していない。
/// 大文字・小文字判定は既存IdeaNest検索（<see cref="NestSuite.IdeaNest.ViewModels.FilterViewModel"/>）
/// と同じ <see cref="StringComparison.OrdinalIgnoreCase"/> を使用し、検索語は既存検索と同様に
/// 前後空白をTrimしてから使う。正規表現は使用しない（通常のIndexOf一致）。
/// </summary>
public static class IdeaNestSearchHighlightHelper
{
    public static IReadOnlyList<IdeaNestHighlightSegment> Split(string? text, string? query)
    {
        var source = text ?? string.Empty;
        var trimmedQuery = (query ?? string.Empty).Trim();

        if (trimmedQuery.Length == 0 || source.Length == 0)
            return new[] { new IdeaNestHighlightSegment(source, false) };

        var segments = new List<IdeaNestHighlightSegment>();
        var remaining = source;
        var foundAny = false;

        while (remaining.Length > 0)
        {
            var part = SearchMatchSegments.Split(remaining, trimmedQuery, StringComparison.OrdinalIgnoreCase);
            if (!part.HasMatch)
            {
                segments.Add(new IdeaNestHighlightSegment(remaining, false));
                break;
            }

            foundAny = true;
            if (part.Before.Length > 0)
                segments.Add(new IdeaNestHighlightSegment(part.Before, false));
            segments.Add(new IdeaNestHighlightSegment(part.Match, true));
            remaining = part.After;
        }

        return foundAny ? segments : new[] { new IdeaNestHighlightSegment(source, false) };
    }
}
