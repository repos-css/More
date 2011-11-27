﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using More.Model;
using More.Parser;
using More.Helpers;
using System.Threading;
using System.Collections.Concurrent;
using System.IO.Compression;

namespace More.Compiler
{
    class StoppedCompilingException : Exception { }

    partial class Compiler
    {
        private static readonly Compiler Singleton = new Compiler();

        public bool Compile(string currentDir, string inputFile, TextReader @in, TextWriter output, IFileLookup lookup)
        {
            try
            {
                Current.SetWorkingDirectory(currentDir);

                List<SpriteExport> spriteExports;

                var initial = ParseStream(inputFile, @in);

                if (initial == null) { return false; }

                if (Current.HasErrors()) { return false; }

                var flattened = EvaluateUsings(inputFile, initial, lookup);

                if (Current.HasErrors()) { return false; }

                VerifyVariableReferences(flattened);

                if (Current.HasErrors()) { return false; }

                var valid = ValidateCharsets(flattened);

                if (Current.HasErrors()) { return false; }

                var ordered = MoveCssImports(valid);

                if (Current.HasErrors()) { return false; }

                var sprited = ExportSprites(inputFile, ordered, out spriteExports);

                if (Current.HasErrors()) { return false; }

                var bound = BindAndEvaluateMixins(sprited);

                if (Current.HasErrors()) { return false; }

                var unrolled = UnrollNestedBlocks(bound);

                if (Current.HasErrors()) { return false; }

                var mergedMedia = MergeMedia(unrolled);

                if (Current.HasErrors()) { return false; }

                var included = CopyIncludes(mergedMedia);

                if (Current.HasErrors()) { return false; }

                var evaluated = EvaluateValues(included);

                if (Current.HasErrors()) { return false; }

                var readyToWrite = ResolveImportant(evaluated);

                if (Current.HasErrors()) { return false; }

                var minimal = RemoveNops(readyToWrite);

                if (Current.HasErrors()) { return false; }

                ValidateFontFace(readyToWrite);

                if (Current.HasErrors()) { return false; }

                List<Block> minified;

                if (Current.Options.HasFlag(Options.Minify))
                {
                    minified = Minify(minimal);

                    if (Current.HasErrors()) { return false; }
                }
                else
                {
                    minified = minimal;
                }

                List<Block> compressionOptimized;

                if (Current.Options.HasFlag(Options.OptimizeCompression))
                {
                    compressionOptimized = OptimizeForCompression(minified);

                    if (Current.HasErrors()) { return false; }
                }
                else
                {
                    compressionOptimized = minified;
                }

                var writer = Current.GetWriter(output);
                foreach (var statement in compressionOptimized.Cast<IWritable>())
                {
                    statement.Write(writer);
                }

                if (spriteExports != null)
                {
                    foreach (var sprite in spriteExports)
                    {
                        var @out = sprite.OutputFile.Replace('/', Path.DirectorySeparatorChar);

                        sprite.Sprite.Save(@out);
                        sprite.Sprite.Dispose();

                        Current.SpriteFileWritten(@out);
                    }
                }

                return true;
            }
            catch (StoppedCompilingException)
            {
                return false;
            }
        }

        public static Compiler Get()
        {
            return Singleton;
        }
    }
}
