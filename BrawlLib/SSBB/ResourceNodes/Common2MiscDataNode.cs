﻿using BrawlLib.SSBBTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrawlLib.SSBB.ResourceNodes
{
    public unsafe class Common2MiscDataNode : ARCEntryNode
    {
        public override ResourceType ResourceType { get { return ResourceType.Container; } }
        internal Common2TblHeader* Header { get { return (Common2TblHeader*)WorkingUncompressed.Address; } }

        // Header variables
        bint _offCount = 0;

        public override bool OnInitialize()
        {
            base.OnInitialize();

            _offCount = Header->_OffCount;

            return true;
        }

        private class OffsetPair
        {
            public int dataOffset;
            public int nameOffset;
            public int dataEnd;

            public override string ToString()
            {
                return dataOffset + "-" + dataEnd + ", " + nameOffset;
            }
        }

        public override void OnPopulate()
        {
            VoidPtr baseAddress = WorkingUncompressed.Address + sizeof(Common2TblHeader);

            int dataLength = Header->_DataLength;
            VoidPtr offsetTable = baseAddress + dataLength + Header->_OffCount * 4;
            VoidPtr stringList = offsetTable + Header->_DataTable * 8;
            List<OffsetPair> offsets = new List<OffsetPair>();

            bint* ptr = (bint*)offsetTable;
            for (int i = 0; i < Header->_DataTable; i++)
            {
                OffsetPair o = new OffsetPair();
                o.dataOffset = *(ptr++);
                o.nameOffset = *(ptr++);
                offsets.Add(o);
            }

            offsets = offsets.OrderBy(o => o.dataOffset).ToList();
            for (int i = 1; i < offsets.Count; i++)
            {
                offsets[i - 1].dataEnd = offsets[i].dataOffset;
            }
            offsets[offsets.Count - 1].dataEnd = dataLength;

            foreach (OffsetPair o in offsets)
            {
                if (o.dataEnd <= o.dataOffset)
                    throw new Exception("Invalid data length (less than data offset) in common2 data");

                DataSource source = new DataSource(baseAddress + o.dataOffset, o.dataEnd - o.dataOffset);
                string name = new string((sbyte*)stringList + o.nameOffset);
                ResourceNode node =
                      name.StartsWith("eventStage") ? new EventMatchNode()
                    : name.StartsWith("allstar") ? new AllstarStageTblNode()
                    : name.StartsWith("simpleStage") ? new ClassicStageTblNode()
                    : name == "sndBgmTitleData" ? new SndBgmTitleDataNode()
                    : (ResourceNode)new RawDataNode();
                node.Initialize(this, source);
                node.Name = name;
                node.HasChanged = false;
            }
        }

        public override void OnRebuild(VoidPtr address, int length, bool force)
        {
            // Update base address for children.
            VoidPtr baseAddress = address + sizeof(Common2TblHeader);

            // Initiate header struct
            Common2TblHeader* Header = (Common2TblHeader*)address;
            *Header = new Common2TblHeader();
            Header->_OffCount = _offCount;
            Header->_DataTable = Children.Count;
            Header->_pad0 = Header->_pad1 =
            Header->_pad2 = Header->_pad3 = 0;

            Dictionary<ResourceNode, VoidPtr> dataLocations = new Dictionary<ResourceNode, VoidPtr>();

            VoidPtr ptr = baseAddress;
            foreach (var child in Children)
            {
                int size = child.CalculateSize(false);
                dataLocations.Add(child, ptr);
                child.Rebuild(ptr, size, false);
                ptr += size;
            }
            Header->_DataLength = (int)(ptr - baseAddress);

            bint* dataPointers = (bint*)ptr;
            bint* stringPointers = dataPointers + 1;
            byte* strings = (byte*)(dataPointers + Children.Count + Children.Count);
            byte* currentString = strings;

            foreach (var child in Children)
            {
                *dataPointers = (int)(dataLocations[child] - baseAddress);
                dataPointers += 2;
                *stringPointers = (int)(currentString - strings);
                stringPointers += 2;

                byte[] text = Encoding.UTF8.GetBytes(child.Name);
                fixed (byte* from = text)
                {
                    Memory.Move(currentString, from, (uint)text.Length);
                    currentString += text.Length;
                    *currentString = 0;
                    currentString++;
                }
            }

            Header->_Length = (int)(currentString - address);

            if (Header->_Length != length)
            {
                throw new Exception("Wrong amount of memory allocated for rebuild of common2 data");
            }
        }

        public override int OnCalculateSize(bool force)
        {
            int size = sizeof(Common2TblHeader);
            foreach (ResourceNode node in Children)
            {
                size += node.CalculateSize(true);
                size += Encoding.UTF8.GetByteCount(node.Name) + 1;
            }
            size += (_offCount * 4) + (Children.Count * 8);
            return size;
        }

        internal static ResourceNode TryParse(DataSource source)
        {
            Common2TblHeader* header = (Common2TblHeader*)source.Address;
            return header->_Length == source.Length &&
                header->_DataLength < source.Length &&
                header->_OffCount == 0 // BrawlLib cannot properly rebuild nodes with _OffCount != 0 yet
                ? new Common2MiscDataNode() : null;
        }
    }
}
