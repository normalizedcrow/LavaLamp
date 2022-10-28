#ifndef LAVA_LAMP_SUBREGION_HELPER_INCLUDED
#define LAVA_LAMP_SUBREGION_HELPER_INCLUDED

#include "LavaLampCoreHelper.hlsl"

//Determine Subregion Count For Shader Variants ----------------------------------------------------

#if defined LAVA_LAMP_SUBREGION_COUNT_16
    #define NUM_LAVA_LAMP_SUBREGIONS 16

#elif defined LAVA_LAMP_SUBREGION_COUNT_15
    #define NUM_LAVA_LAMP_SUBREGIONS 15

#elif defined LAVA_LAMP_SUBREGION_COUNT_14
    #define NUM_LAVA_LAMP_SUBREGIONS 14

#elif defined LAVA_LAMP_SUBREGION_COUNT_13
    #define NUM_LAVA_LAMP_SUBREGIONS 13

#elif defined LAVA_LAMP_SUBREGION_COUNT_12
    #define NUM_LAVA_LAMP_SUBREGIONS 12

#elif defined LAVA_LAMP_SUBREGION_COUNT_11
    #define NUM_LAVA_LAMP_SUBREGIONS 11

#elif defined LAVA_LAMP_SUBREGION_COUNT_10
    #define NUM_LAVA_LAMP_SUBREGIONS 10

#elif defined LAVA_LAMP_SUBREGION_COUNT_9
    #define NUM_LAVA_LAMP_SUBREGIONS 9

#elif defined LAVA_LAMP_SUBREGION_COUNT_8
    #define NUM_LAVA_LAMP_SUBREGIONS 8

#elif defined LAVA_LAMP_SUBREGION_COUNT_7
    #define NUM_LAVA_LAMP_SUBREGIONS 7

#elif defined LAVA_LAMP_SUBREGION_COUNT_6
    #define NUM_LAVA_LAMP_SUBREGIONS 6

#elif defined LAVA_LAMP_SUBREGION_COUNT_5
    #define NUM_LAVA_LAMP_SUBREGIONS 5

#elif defined LAVA_LAMP_SUBREGION_COUNT_4
    #define NUM_LAVA_LAMP_SUBREGIONS 4

#elif defined LAVA_LAMP_SUBREGION_COUNT_3
    #define NUM_LAVA_LAMP_SUBREGIONS 3

#elif defined LAVA_LAMP_SUBREGION_COUNT_2
    #define NUM_LAVA_LAMP_SUBREGIONS 2

#else
    #define NUM_LAVA_LAMP_SUBREGIONS 1
#endif

//Declare Subregion Constants ---------------------------------------------------------------------

DECLARE_LAVA_LAMP_CONSTANTS(0);

#if (NUM_LAVA_LAMP_SUBREGIONS > 1)
    DECLARE_LAVA_LAMP_CONSTANTS(1);
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 2)
    DECLARE_LAVA_LAMP_CONSTANTS(2);
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 3)
    DECLARE_LAVA_LAMP_CONSTANTS(3);
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 4)
    DECLARE_LAVA_LAMP_CONSTANTS(4);
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 5)
    DECLARE_LAVA_LAMP_CONSTANTS(5);
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 6)
    DECLARE_LAVA_LAMP_CONSTANTS(6);
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 7)
    DECLARE_LAVA_LAMP_CONSTANTS(7);
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 8)
    DECLARE_LAVA_LAMP_CONSTANTS(8);
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 9)
    DECLARE_LAVA_LAMP_CONSTANTS(9);
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 10)
    DECLARE_LAVA_LAMP_CONSTANTS(10);
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 11)
    DECLARE_LAVA_LAMP_CONSTANTS(11);
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 12)
    DECLARE_LAVA_LAMP_CONSTANTS(12);
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 13)
    DECLARE_LAVA_LAMP_CONSTANTS(13);
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 14)
    DECLARE_LAVA_LAMP_CONSTANTS(14);
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 15)
    DECLARE_LAVA_LAMP_CONSTANTS(15);
#endif

//Setup Parameter Helpers -------------------------------------------------------------------------

LavaLampShapeParameters GetLavaLampShapeParameters(int index)
{
    LavaLampShapeParameters shapeParameters;

    [branch]
    switch (index)
    {
    default:
        FILL_LAVA_LAMP_SHAPE_PARAMETERS(shapeParameters, 0);
        break;

#if (NUM_LAVA_LAMP_SUBREGIONS > 1)
    case 1:
        FILL_LAVA_LAMP_SHAPE_PARAMETERS(shapeParameters, 1);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 2)
    case 2:
        FILL_LAVA_LAMP_SHAPE_PARAMETERS(shapeParameters, 2);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 3)
    case 3:
        FILL_LAVA_LAMP_SHAPE_PARAMETERS(shapeParameters, 3);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 4)
    case 4:
        FILL_LAVA_LAMP_SHAPE_PARAMETERS(shapeParameters, 4);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 5)
    case 5:
        FILL_LAVA_LAMP_SHAPE_PARAMETERS(shapeParameters, 5);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 6)
    case 6:
        FILL_LAVA_LAMP_SHAPE_PARAMETERS(shapeParameters, 6);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 7)
    case 7:
        FILL_LAVA_LAMP_SHAPE_PARAMETERS(shapeParameters, 7);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 8)
    case 8:
        FILL_LAVA_LAMP_SHAPE_PARAMETERS(shapeParameters, 8);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 9)
    case 9:
        FILL_LAVA_LAMP_SHAPE_PARAMETERS(shapeParameters, 9);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 10)
    case 10:
        FILL_LAVA_LAMP_SHAPE_PARAMETERS(shapeParameters, 10);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 11)
    case 11:
        FILL_LAVA_LAMP_SHAPE_PARAMETERS(shapeParameters, 11);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 12)
    case 12:
        FILL_LAVA_LAMP_SHAPE_PARAMETERS(shapeParameters, 12);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 13)
    case 13:
        FILL_LAVA_LAMP_SHAPE_PARAMETERS(shapeParameters, 13);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 14)
    case 14:
        FILL_LAVA_LAMP_SHAPE_PARAMETERS(shapeParameters, 14);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 15)
    case 15:
        FILL_LAVA_LAMP_SHAPE_PARAMETERS(shapeParameters, 15);
        break;
#endif
    }

    return shapeParameters;
}

LavaLampMaterialParameters GetLavaLampMaterialParameters(int index)
{
    LavaLampMaterialParameters materialParameters;

    [branch]
    switch (index)
    {
    default:
        FILL_LAVA_LAMP_MATERIAL_PARAMETERS(materialParameters, 0);
        break;

#if (NUM_LAVA_LAMP_SUBREGIONS > 1)
    case 1:
        FILL_LAVA_LAMP_MATERIAL_PARAMETERS(materialParameters, 1);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 2)
    case 2:
        FILL_LAVA_LAMP_MATERIAL_PARAMETERS(materialParameters, 2);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 3)
    case 3:
        FILL_LAVA_LAMP_MATERIAL_PARAMETERS(materialParameters, 3);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 4)
    case 4:
        FILL_LAVA_LAMP_MATERIAL_PARAMETERS(materialParameters, 4);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 5)
    case 5:
        FILL_LAVA_LAMP_MATERIAL_PARAMETERS(materialParameters, 5);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 6)
    case 6:
        FILL_LAVA_LAMP_MATERIAL_PARAMETERS(materialParameters, 6);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 7)
    case 7:
        FILL_LAVA_LAMP_MATERIAL_PARAMETERS(materialParameters, 7);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 8)
    case 8:
        FILL_LAVA_LAMP_MATERIAL_PARAMETERS(materialParameters, 8);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 9)
    case 9:
        FILL_LAVA_LAMP_MATERIAL_PARAMETERS(materialParameters, 9);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 10)
    case 10:
        FILL_LAVA_LAMP_MATERIAL_PARAMETERS(materialParameters, 10);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 11)
    case 11:
        FILL_LAVA_LAMP_MATERIAL_PARAMETERS(materialParameters, 11);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 12)
    case 12:
        FILL_LAVA_LAMP_MATERIAL_PARAMETERS(materialParameters, 12);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 13)
    case 13:
        FILL_LAVA_LAMP_MATERIAL_PARAMETERS(materialParameters, 13);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 14)
    case 14:
        FILL_LAVA_LAMP_MATERIAL_PARAMETERS(materialParameters, 14);
        break;
#endif

#if (NUM_LAVA_LAMP_SUBREGIONS > 15)
    case 15:
        FILL_LAVA_LAMP_MATERIAL_PARAMETERS(materialParameters, 15);
        break;
#endif
    }

    return materialParameters;
};

#endif