﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using More.Model;

namespace More.Compiler
{
    partial class Compiler
    {
        private static List<Block> RemoveNops(List<Block> readyToWrite)
        {
            var ret = new List<Block>();

            // Remove no-ops
            foreach (var statement in readyToWrite)
            {
                var media = statement as MediaBlock;

                if (media == null)
                {
                    ret.Add(statement);
                    continue;
                }

                var subStatements = media.Blocks.ToList();

                subStatements.RemoveAll(a =>
                {
                    var block = a as SelectorAndBlock;
                    if (block == null) return false;

                    return block.Properties.Count() == 0;
                }
                );

                ret.Add(new MediaBlock(media.ForMedia.ToList(), subStatements, media.Start, media.Stop, media.FilePath));
            }

            ret =
                ret.Select(
                    delegate(Block x)
                    {
                        var keyframes = x as KeyFramesBlock;
                        if (keyframes == null) return x;

                        var frames = keyframes.Frames.Where(f => f.Properties.Count() > 0).ToList();

                        return new KeyFramesBlock(keyframes.Prefix, keyframes.Name, frames, keyframes.Variables.ToList(), keyframes.Start, keyframes.Stop, keyframes.FilePath);
                    }
                ).ToList();

            ret.RemoveAll(a =>
            {
                var block = a as SelectorAndBlock;
                var media = a as MediaBlock;
                var keyframes = a as KeyFramesBlock;
                if (block == null && media == null && keyframes == null) return false;

                if (block != null) return block.Properties.Count() == 0;
                if (media != null) return media.Blocks.Count() == 0;
                if (keyframes != null) return keyframes.Frames.Count() == 0;

                throw new InvalidOperationException();
            }
            );

            return ret;
        }
    }
}
