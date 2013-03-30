﻿#region Using

using System;

#endregion

namespace Libra.Graphics
{
    public partial class BasicEffect
    {
        static readonly byte[] VSBasicTx =
        {
             68,  88,  66,  67, 160,  62, 
             87, 247, 158, 198,  70,  98, 
            102, 170,  42, 202, 103, 175, 
             31, 164,   1,   0,   0,   0, 
            176,   3,   0,   0,   4,   0, 
              0,   0,  48,   0,   0,   0, 
            112,   1,   0,   0, 204,   2, 
              0,   0,  36,   3,   0,   0, 
             65, 111, 110,  57,  56,   1, 
              0,   0,  56,   1,   0,   0, 
              0,   2, 254, 255, 236,   0, 
              0,   0,  76,   0,   0,   0, 
              3,   0,  36,   0,   0,   0, 
             72,   0,   0,   0,  72,   0, 
              0,   0,  36,   0,   1,   0, 
             72,   0,   0,   0,   0,   0, 
              1,   0,   1,   0,   0,   0, 
              0,   0,   0,   0,  14,   0, 
              1,   0,   2,   0,   0,   0, 
              0,   0,   0,   0,  22,   0, 
              4,   0,   3,   0,   0,   0, 
              0,   0,   0,   0,   0,   0, 
              0,   2, 254, 255,  81,   0, 
              0,   5,   7,   0,  15, 160, 
              0,   0,   0,   0,   0,   0, 
            128,  63,   0,   0,   0,   0, 
              0,   0,   0,   0,  31,   0, 
              0,   2,   5,   0,   0, 128, 
              0,   0,  15, 144,  31,   0, 
              0,   2,   5,   0,   1, 128, 
              1,   0,  15, 144,   9,   0, 
              0,   3,   0,   0,   4, 192, 
              0,   0, 228, 144,   5,   0, 
            228, 160,   9,   0,   0,   3, 
              0,   0,   1, 128,   0,   0, 
            228, 144,   2,   0, 228, 160, 
             11,   0,   0,   3,   0,   0, 
              1, 128,   0,   0,   0, 128, 
              7,   0,   0, 160,  10,   0, 
              0,   3,   1,   0,   8, 224, 
              0,   0,   0, 128,   7,   0, 
             85, 160,   9,   0,   0,   3, 
              0,   0,   1, 128,   0,   0, 
            228, 144,   3,   0, 228, 160, 
              9,   0,   0,   3,   0,   0, 
              2, 128,   0,   0, 228, 144, 
              4,   0, 228, 160,   9,   0, 
              0,   3,   0,   0,   4, 128, 
              0,   0, 228, 144,   6,   0, 
            228, 160,   4,   0,   0,   4, 
              0,   0,   3, 192,   0,   0, 
            170, 128,   0,   0, 228, 160, 
              0,   0, 228, 128,   1,   0, 
              0,   2,   0,   0,   8, 192, 
              0,   0, 170, 128,   1,   0, 
              0,   2,   0,   0,  15, 224, 
              1,   0, 228, 160,   1,   0, 
              0,   2,   1,   0,   7, 224, 
              7,   0,   0, 160,   1,   0, 
              0,   2,   2,   0,   3, 224, 
              1,   0, 228, 144, 255, 255, 
              0,   0,  83,  72,  68,  82, 
             84,   1,   0,   0,  64,   0, 
              1,   0,  85,   0,   0,   0, 
             89,   0,   0,   4,  70, 142, 
             32,   0,   0,   0,   0,   0, 
             26,   0,   0,   0,  95,   0, 
              0,   3, 242,  16,  16,   0, 
              0,   0,   0,   0,  95,   0, 
              0,   3,  50,  16,  16,   0, 
              1,   0,   0,   0, 101,   0, 
              0,   3, 242,  32,  16,   0, 
              0,   0,   0,   0, 101,   0, 
              0,   3, 242,  32,  16,   0, 
              1,   0,   0,   0, 101,   0, 
              0,   3,  50,  32,  16,   0, 
              2,   0,   0,   0, 103,   0, 
              0,   4, 242,  32,  16,   0, 
              3,   0,   0,   0,   1,   0, 
              0,   0,  54,   0,   0,   6, 
            242,  32,  16,   0,   0,   0, 
              0,   0,  70, 142,  32,   0, 
              0,   0,   0,   0,   0,   0, 
              0,   0,  17,  32,   0,   8, 
            130,  32,  16,   0,   1,   0, 
              0,   0,  70,  30,  16,   0, 
              0,   0,   0,   0,  70, 142, 
             32,   0,   0,   0,   0,   0, 
             14,   0,   0,   0,  54,   0, 
              0,   8, 114,  32,  16,   0, 
              1,   0,   0,   0,   2,  64, 
              0,   0,   0,   0,   0,   0, 
              0,   0,   0,   0,   0,   0, 
              0,   0,   0,   0,   0,   0, 
             54,   0,   0,   5,  50,  32, 
             16,   0,   2,   0,   0,   0, 
             70,  16,  16,   0,   1,   0, 
              0,   0,  17,   0,   0,   8, 
             18,  32,  16,   0,   3,   0, 
              0,   0,  70,  30,  16,   0, 
              0,   0,   0,   0,  70, 142, 
             32,   0,   0,   0,   0,   0, 
             22,   0,   0,   0,  17,   0, 
              0,   8,  34,  32,  16,   0, 
              3,   0,   0,   0,  70,  30, 
             16,   0,   0,   0,   0,   0, 
             70, 142,  32,   0,   0,   0, 
              0,   0,  23,   0,   0,   0, 
             17,   0,   0,   8,  66,  32, 
             16,   0,   3,   0,   0,   0, 
             70,  30,  16,   0,   0,   0, 
              0,   0,  70, 142,  32,   0, 
              0,   0,   0,   0,  24,   0, 
              0,   0,  17,   0,   0,   8, 
            130,  32,  16,   0,   3,   0, 
              0,   0,  70,  30,  16,   0, 
              0,   0,   0,   0,  70, 142, 
             32,   0,   0,   0,   0,   0, 
             25,   0,   0,   0,  62,   0, 
              0,   1,  73,  83,  71,  78, 
             80,   0,   0,   0,   2,   0, 
              0,   0,   8,   0,   0,   0, 
             56,   0,   0,   0,   0,   0, 
              0,   0,   0,   0,   0,   0, 
              3,   0,   0,   0,   0,   0, 
              0,   0,  15,  15,   0,   0, 
             68,   0,   0,   0,   0,   0, 
              0,   0,   0,   0,   0,   0, 
              3,   0,   0,   0,   1,   0, 
              0,   0,   3,   3,   0,   0, 
             83,  86,  95,  80, 111, 115, 
            105, 116, 105, 111, 110,   0, 
             84,  69,  88,  67,  79,  79, 
             82,  68,   0, 171, 171, 171, 
             79,  83,  71,  78, 132,   0, 
              0,   0,   4,   0,   0,   0, 
              8,   0,   0,   0, 104,   0, 
              0,   0,   0,   0,   0,   0, 
              0,   0,   0,   0,   3,   0, 
              0,   0,   0,   0,   0,   0, 
             15,   0,   0,   0, 104,   0, 
              0,   0,   1,   0,   0,   0, 
              0,   0,   0,   0,   3,   0, 
              0,   0,   1,   0,   0,   0, 
             15,   0,   0,   0, 110,   0, 
              0,   0,   0,   0,   0,   0, 
              0,   0,   0,   0,   3,   0, 
              0,   0,   2,   0,   0,   0, 
              3,  12,   0,   0, 119,   0, 
              0,   0,   0,   0,   0,   0, 
              1,   0,   0,   0,   3,   0, 
              0,   0,   3,   0,   0,   0, 
             15,   0,   0,   0,  67,  79, 
             76,  79,  82,   0,  84,  69, 
             88,  67,  79,  79,  82,  68, 
              0,  83,  86,  95,  80, 111, 
            115, 105, 116, 105, 111, 110, 
              0, 171
        };
    }
}
