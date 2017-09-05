﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public static class ViewEnginePath
    {
        public static readonly char[] PathSeparators = new[] { '/', '\\' };
        private const string CurrentDirectoryToken = ".";
        private const string ParentDirectoryToken = "..";
        private static readonly string[] TokensRequiringNormalization = new string[]
        {
            // ./
            CurrentDirectoryToken + PathSeparators[0],
            // .\
            CurrentDirectoryToken + PathSeparators[1],
            // ../
            ParentDirectoryToken + PathSeparators[0],
            // ..\
            ParentDirectoryToken + PathSeparators[1],
            // //
            "" + PathSeparators[0] + PathSeparators[0],
            // \\
            "" + PathSeparators[1] + PathSeparators[1],
        };

        public static string CombinePath(string first, string second)
        {
            Debug.Assert(!string.IsNullOrEmpty(first));

            if (second.StartsWith("/", StringComparison.Ordinal))
            {
                // "second" is already an app-rooted path. Return it as-is.
                return second;
            }

            string result;
           
            // Get directory name (including final slash) but do not use Path.GetDirectoryName() to preserve path
            // normalization.
            var index = first.LastIndexOf('/');
            Debug.Assert(index >= 0);

            if (index == first.Length - 1)
            {
                // If the first ends in a trailing slash e.g. "/Home/", assume it's a directory.
                result = first + second;
            }
            else
            {
                result = first.Substring(0, index + 1) + second;
            }

            return NormalizePath(result);
        }

        public static string NormalizePath(string path)
        {
            if (!RequiresPathNormalization(path))
            {
                return path;
            }

            var pathSegments = new List<StringSegment>();
            var tokenizer = new StringTokenizer(path, PathSeparators);
            foreach (var segment in tokenizer)
            {
                if (segment.Length == 0)
                {
                    // Ignore multiple directory separators
                    continue;
                }
                if (segment.Equals(ParentDirectoryToken, StringComparison.Ordinal))
                {
                    if (pathSegments.Count == 0)
                    {
                        // Don't resolve the path if we ever escape the file system root. We can't reason about it in a
                        // consistent way.
                        return path;
                    }
                    pathSegments.RemoveAt(pathSegments.Count - 1);
                }
                else if (segment.Equals(CurrentDirectoryToken, StringComparison.Ordinal))
                {
                    // We already have the current directory
                    continue;
                }
                else
                {
                    pathSegments.Add(segment);
                }
            }

            var builder = new StringBuilder();
            for (var i = 0; i < pathSegments.Count; i++)
            {
                var segment = pathSegments[i];
                builder.Append('/');
                builder.Append(segment.Buffer, segment.Offset, segment.Length);
            }

            return builder.ToString();
        }

        private static bool RequiresPathNormalization(string path)
        {
            for (var i = 0; i < TokensRequiringNormalization.Length; i++)
            {
                if (path.IndexOf(TokensRequiringNormalization[i]) != -1)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
