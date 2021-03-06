﻿#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public struct InputElement : IEquatable<InputElement>
    {
        // D3D では uint で 0xffffffff。
        // int の -1 は、uint へのキャストで 0xffffffff となる。
        public const int AppendAlignedElement = -1;

        public string SemanticName;

        public int SemanticIndex;

        public VertexFormat Format;

        public int InputSlot;

        public int AlignedByteOffset;

        public bool PerInstance;

        public int InstanceDataStepRate;

        public int SizeInBytes
        {
            get { return FormatHelper.SizeInBytes(Format); }
        }

        public InputElement(string semanticName, VertexFormat format,
            int inputSlot = 0, int alignedByteOffset = AppendAlignedElement,
            bool perInstance = false, int instanceDataStepRate = 0)
        {
            SemanticName = semanticName;
            SemanticIndex = 0;
            Format = format;
            InputSlot = inputSlot;
            AlignedByteOffset = alignedByteOffset;
            PerInstance = perInstance;
            InstanceDataStepRate = instanceDataStepRate;
        }

        public InputElement(string semanticName, int semanticIndex, VertexFormat format,
            int inputSlot = 0, int alignedByteOffset = AppendAlignedElement,
            bool perInstance = false, int instanceDataStepRate = 0)
        {
            SemanticName = semanticName;
            SemanticIndex = semanticIndex;
            Format = format;
            InputSlot = inputSlot;
            AlignedByteOffset = alignedByteOffset;
            PerInstance = perInstance;
            InstanceDataStepRate = instanceDataStepRate;
        }

        #region Equatable

        public static bool operator ==(InputElement value1, InputElement value2)
        {
            return value1.Equals(value2);
        }

        public static bool operator !=(InputElement value1, InputElement value2)
        {
            return !value1.Equals(value2);
        }

        public bool Equals(InputElement other)
        {
            return SemanticName == other.SemanticName && SemanticIndex == other.SemanticIndex &&
                Format == other.Format &&
                InputSlot == other.InputSlot && AlignedByteOffset == other.AlignedByteOffset &&
                PerInstance == other.PerInstance && InstanceDataStepRate == other.InstanceDataStepRate;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) return false;

            return Equals((InputElement) obj);
        }

        public override int GetHashCode()
        {
            return SemanticName.GetHashCode() ^ SemanticIndex.GetHashCode() ^
                Format.GetHashCode() ^ InputSlot.GetHashCode() ^ AlignedByteOffset.GetHashCode() ^
                PerInstance.GetHashCode() ^ InstanceDataStepRate.GetHashCode();
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return "{SemanticName:" + SemanticName + " SemanticIndex:" + SemanticIndex +
                " Format:" + Format +
                " InputSlot:" + InputSlot + " AlignedByteOffset:" + AlignedByteOffset +
                " PerInstance:" + PerInstance + " InstanceDataStepRate:" + InstanceDataStepRate +
                "]";
        }

        #endregion
    }
}
