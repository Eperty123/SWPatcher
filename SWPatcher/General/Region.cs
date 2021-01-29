﻿/*
 * This file is part of Closers Patcher.
 * Copyright (C) 2017 Miyu
 * 
 * Closers Patcher is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Closers Patcher is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Closers Patcher. If not, see <http://www.gnu.org/licenses/>.
 */

namespace SWPatcher.General
{
    internal class Region
    {
        internal string Id { get; }
        internal string Name { get; }
        internal string Folder { get; }
        internal Language[] AppliedLanguages { get; }

        internal Region(string id)
        {
            Id = id;
        }

        internal Region(string id, string name, string folder, Language[] appliedLanguages) : this(id)
        {
            Name = name;
            Folder = folder;
            AppliedLanguages = appliedLanguages;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var region = obj as Region;
            return Id == region.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
