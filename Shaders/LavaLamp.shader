Shader "normalizedcrow/Lava Lamp"
{
    Properties
    {
        //Glass
        _Reflectiveness("Reflectiveness", Range(0.0, 1.0)) = 0.1
        _RoughnessMap("Roughness Map", 2D) = "black" {}
        _MinPerceptualRoughness("Min Roughness", Range(0.0, 1.0)) = 0.1
        _MaxPerceptualRoughness("Max Roughness", Range(0.0, 1.0)) = 1.0
        [Normal] _NormalMap("Normal Map", 2D) = "bump" {}
        _NormalStrength("Normal Map Strength", Range(0.0, 1.0)) = 1.0
        _TintMap("Tint Map", 2D) = "white" {}
        _Tint("Tint Multiplier", Color) = (1.0, 1.0, 1.0, 1.0)
        _RefractiveIndex("Refractive Index", Range(1.0, 1.5)) = 1.1
        [HDR] _BackgroundColor("Background Color", Color) = (0.0, 0.0, 0.0, 0.0)

        //Lava Shared Properties
        _LavaPadding("Padding", Range(0.01, 0.2)) = 0.05
        _LavaSmoothingFactor("Smoothing", Range(0.01, 0.5)) = 0.3
        _LavaVerticalSeparation("Vertical Spacing", Range(1.0, 5.0)) = 2.5
        _LavaSkipChance("Invisible Blob Chance", Range(0.0, 1.0)) = 0.3
        _LavaMinSize("Min Size", Range(0.0, 1.0)) = 0.25
        _LavaMaxSize("Max Size", Range(0.0, 1.0)) = 1.0
        _LavaSizeDistribution("Size Distribution", Range(-3.0, 3.0)) = 0.0
        _LavaMinSpeed("Min Speed", Range(0.0, 1.0)) = 0.05
        _LavaMaxSpeed("Max Speed", Range(0.0, 1.0)) = 0.2
        _LavaMinDriftSpeed("Min Drift Speed", Range(0.0, 5.0)) = 0.0
        _LavaMaxDriftSpeed("Max Drift Speed", Range(0.0, 5.0)) = 1.0
        _LavaReflectiveness("Reflectiveness", Range(0.0, 1.0)) = 0.0
        _LavaPerceptualRoughness("Roughness", Range(0.0, 1.0)) = 0.2
        _LavaSoftDepthSize("Soft Depth Size", Range(0.0, 1.0)) = 0.01
        
        //Subregion 0
        [PowerSlider(2.72)] _LavaScale0("Scale", Range(0.001, 2.0)) = 0.5
        _LavaTopReservoirHeight0("Top Reservoir Height", Float) = 1.0
        _LavaBottomReservoirHeight0("Bottom Reservoir Height", Float) = -1.0
        _LavaCoreColor0("Lava Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaEdgeColor0("Lava Edge Color", Color) = (0.75, 0.75, 0.75, 1.0)
        [PowerSlider(2.72)] _LavaColorThicknessScale0("Lave Color Thickness Scale", Range(0.0, 30.0)) = 5.0
        _LavaWaterHazeColor0("Water Haze Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterHazeStrength0("Water Haze Density", Range(0.0, 50.0)) = 0.0
        _LavaWaterTintColor0("Water Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterTintStrength0("Water Tint Strength", Range(1.0, 100.0)) = 1.0
        [HDR] _LavaTopLightColor0("Top Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaTopLightHeight0("Top Light Height", Float) = 1.0
        [HDR] _LavaBottomLightColor0("Bottom Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaBottomLightHeight0("Bottom Light Height", Float) = -1.0
        [PowerSlider(2.72)] _LavaLightFalloff0("Light Falloff Scale", Range(0.0, 100.0)) = 1.0
        _LavaFlowDirection0("Flow Direction", Vector) = (0.0, 1.0, 0.0, 0.0)

        //Subregion 1
        [PowerSlider(2.72)] _LavaScale1("Scale", Range(0.001, 2.0)) = 0.5
        _LavaTopReservoirHeight1("Top Reservoir Height", Float) = 1.0
        _LavaBottomReservoirHeight1("Bottom Reservoir Height", Float) = -1.0
        _LavaCoreColor1("Lava Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaEdgeColor1("Lava Edge Color", Color) = (0.75, 0.75, 0.75, 1.0)
        [PowerSlider(2.72)] _LavaColorThicknessScale1("Lave Color Thickness Scale", Range(0.0, 30.0)) = 5.0
        _LavaWaterHazeColor1("Water Haze Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterHazeStrength1("Water Haze Density", Range(0.0, 50.0)) = 0.0
        _LavaWaterTintColor1("Water Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterTintStrength1("Water Tint Strength", Range(1.0, 100.0)) = 1.0
        [HDR] _LavaTopLightColor1("Top Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaTopLightHeight1("Top Light Height", Float) = 1.0
        [HDR] _LavaBottomLightColor1("Bottom Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaBottomLightHeight1("Bottom Light Height", Float) = -1.0
        [PowerSlider(2.72)] _LavaLightFalloff1("Light Falloff Scale", Range(0.0, 100.0)) = 1.0
        _LavaFlowDirection1("Flow Direction", Vector) = (0.0, 1.0, 0.0, 0.0)

        //Subregion 2
        [PowerSlider(2.72)] _LavaScale2("Scale", Range(0.001, 2.0)) = 0.5
        _LavaTopReservoirHeight2("Top Reservoir Height", Float) = 1.0
        _LavaBottomReservoirHeight2("Bottom Reservoir Height", Float) = -1.0
        _LavaCoreColor2("Lava Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaEdgeColor2("Lava Edge Color", Color) = (0.75, 0.75, 0.75, 1.0)
        [PowerSlider(2.72)] _LavaColorThicknessScale2("Lave Color Thickness Scale", Range(0.0, 30.0)) = 5.0
        _LavaWaterHazeColor2("Water Haze Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterHazeStrength2("Water Haze Density", Range(0.0, 50.0)) = 0.0
        _LavaWaterTintColor2("Water Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterTintStrength2("Water Tint Strength", Range(1.0, 100.0)) = 1.0
        [HDR] _LavaTopLightColor2("Top Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaTopLightHeight2("Top Light Height", Float) = 1.0
        [HDR] _LavaBottomLightColor2("Bottom Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaBottomLightHeight2("Bottom Light Height", Float) = -1.0
        [PowerSlider(2.72)] _LavaLightFalloff2("Light Falloff Scale", Range(0.0, 100.0)) = 1.0
        _LavaFlowDirection2("Flow Direction", Vector) = (0.0, 1.0, 0.0, 0.0)

        //Subregion 3
        [PowerSlider(2.72)] _LavaScale3("Scale", Range(0.001, 2.0)) = 0.5
        _LavaTopReservoirHeight3("Top Reservoir Height", Float) = 1.0
        _LavaBottomReservoirHeight3("Bottom Reservoir Height", Float) = -1.0
        _LavaCoreColor3("Lava Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaEdgeColor3("Lava Edge Color", Color) = (0.75, 0.75, 0.75, 1.0)
        [PowerSlider(2.72)] _LavaColorThicknessScale3("Lave Color Thickness Scale", Range(0.0, 30.0)) = 5.0
        _LavaWaterHazeColor3("Water Haze Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterHazeStrength3("Water Haze Density", Range(0.0, 50.0)) = 0.0
        _LavaWaterTintColor3("Water Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterTintStrength3("Water Tint Strength", Range(1.0, 100.0)) = 1.0
        [HDR] _LavaTopLightColor3("Top Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaTopLightHeight3("Top Light Height", Float) = 1.0
        [HDR] _LavaBottomLightColor3("Bottom Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaBottomLightHeight3("Bottom Light Height", Float) = -1.0
        [PowerSlider(2.72)] _LavaLightFalloff3("Light Falloff Scale", Range(0.0, 100.0)) = 1.0
        _LavaFlowDirection3("Flow Direction", Vector) = (0.0, 1.0, 0.0, 0.0)

        //Subregion 4
        [PowerSlider(2.72)] _LavaScale4("Scale", Range(0.001, 2.0)) = 0.5
        _LavaTopReservoirHeight4("Top Reservoir Height", Float) = 1.0
        _LavaBottomReservoirHeight4("Bottom Reservoir Height", Float) = -1.0
        _LavaCoreColor4("Lava Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaEdgeColor4("Lava Edge Color", Color) = (0.75, 0.75, 0.75, 1.0)
        [PowerSlider(2.72)] _LavaColorThicknessScale4("Lave Color Thickness Scale", Range(0.0, 30.0)) = 5.0
        _LavaWaterHazeColor4("Water Haze Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterHazeStrength4("Water Haze Density", Range(0.0, 50.0)) = 0.0
        _LavaWaterTintColor4("Water Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterTintStrength4("Water Tint Strength", Range(1.0, 100.0)) = 1.0
        [HDR] _LavaTopLightColor4("Top Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaTopLightHeight4("Top Light Height", Float) = 1.0
        [HDR] _LavaBottomLightColor4("Bottom Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaBottomLightHeight4("Bottom Light Height", Float) = -1.0
        [PowerSlider(2.72)] _LavaLightFalloff4("Light Falloff Scale", Range(0.0, 100.0)) = 1.0
        _LavaFlowDirection4("Flow Direction", Vector) = (0.0, 1.0, 0.0, 0.0)

        //Subregion 5
        [PowerSlider(2.72)] _LavaScale5("Scale", Range(0.001, 2.0)) = 0.5
        _LavaTopReservoirHeight5("Top Reservoir Height", Float) = 1.0
        _LavaBottomReservoirHeight5("Bottom Reservoir Height", Float) = -1.0
        _LavaCoreColor5("Lava Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaEdgeColor5("Lava Edge Color", Color) = (0.75, 0.75, 0.75, 1.0)
        [PowerSlider(2.72)] _LavaColorThicknessScale5("Lave Color Thickness Scale", Range(0.0, 30.0)) = 5.0
        _LavaWaterHazeColor5("Water Haze Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterHazeStrength5("Water Haze Density", Range(0.0, 50.0)) = 0.0
        _LavaWaterTintColor5("Water Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterTintStrength5("Water Tint Strength", Range(1.0, 100.0)) = 1.0
        [HDR] _LavaTopLightColor5("Top Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaTopLightHeight5("Top Light Height", Float) = 1.0
        [HDR] _LavaBottomLightColor5("Bottom Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaBottomLightHeight5("Bottom Light Height", Float) = -1.0
        [PowerSlider(2.72)] _LavaLightFalloff5("Light Falloff Scale", Range(0.0, 100.0)) = 1.0
        _LavaFlowDirection5("Flow Direction", Vector) = (0.0, 1.0, 0.0, 0.0)

        //Subregion 6
        [PowerSlider(2.72)] _LavaScale6("Scale", Range(0.001, 2.0)) = 0.5
        _LavaTopReservoirHeight6("Top Reservoir Height", Float) = 1.0
        _LavaBottomReservoirHeight6("Bottom Reservoir Height", Float) = -1.0
        _LavaCoreColor6("Lava Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaEdgeColor6("Lava Edge Color", Color) = (0.75, 0.75, 0.75, 1.0)
        [PowerSlider(2.72)] _LavaColorThicknessScale6("Lave Color Thickness Scale", Range(0.0, 30.0)) = 5.0
        _LavaWaterHazeColor6("Water Haze Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterHazeStrength6("Water Haze Density", Range(0.0, 50.0)) = 0.0
        _LavaWaterTintColor6("Water Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterTintStrength6("Water Tint Strength", Range(1.0, 100.0)) = 1.0
        [HDR] _LavaTopLightColor6("Top Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaTopLightHeight6("Top Light Height", Float) = 1.0
        [HDR] _LavaBottomLightColor6("Bottom Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaBottomLightHeight6("Bottom Light Height", Float) = -1.0
        [PowerSlider(2.72)] _LavaLightFalloff6("Light Falloff Scale", Range(0.0, 100.0)) = 1.0
        _LavaFlowDirection6("Flow Direction", Vector) = (0.0, 1.0, 0.0, 0.0)

        //Subregion 7
        [PowerSlider(2.72)] _LavaScale7("Scale", Range(0.001, 2.0)) = 0.5
        _LavaTopReservoirHeight7("Top Reservoir Height", Float) = 1.0
        _LavaBottomReservoirHeight7("Bottom Reservoir Height", Float) = -1.0
        _LavaCoreColor7("Lava Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaEdgeColor7("Lava Edge Color", Color) = (0.75, 0.75, 0.75, 1.0)
        [PowerSlider(2.72)] _LavaColorThicknessScale7("Lave Color Thickness Scale", Range(0.0, 30.0)) = 5.0
        _LavaWaterHazeColor7("Water Haze Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterHazeStrength7("Water Haze Density", Range(0.0, 50.0)) = 0.0
        _LavaWaterTintColor7("Water Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterTintStrength7("Water Tint Strength", Range(1.0, 100.0)) = 1.0
        [HDR] _LavaTopLightColor7("Top Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaTopLightHeight7("Top Light Height", Float) = 1.0
        [HDR] _LavaBottomLightColor7("Bottom Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaBottomLightHeight7("Bottom Light Height", Float) = -1.0
        [PowerSlider(2.72)] _LavaLightFalloff7("Light Falloff Scale", Range(0.0, 100.0)) = 1.0
        _LavaFlowDirection7("Flow Direction", Vector) = (0.0, 1.0, 0.0, 0.0)

        //Subregion 8
        [PowerSlider(2.72)] _LavaScale8("Scale", Range(0.001, 2.0)) = 0.5
        _LavaTopReservoirHeight8("Top Reservoir Height", Float) = 1.0
        _LavaBottomReservoirHeight8("Bottom Reservoir Height", Float) = -1.0
        _LavaCoreColor8("Lava Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaEdgeColor8("Lava Edge Color", Color) = (0.75, 0.75, 0.75, 1.0)
        [PowerSlider(2.72)] _LavaColorThicknessScale8("Lave Color Thickness Scale", Range(0.0, 30.0)) = 5.0
        _LavaWaterHazeColor8("Water Haze Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterHazeStrength8("Water Haze Density", Range(0.0, 50.0)) = 0.0
        _LavaWaterTintColor8("Water Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterTintStrength8("Water Tint Strength", Range(1.0, 100.0)) = 1.0
        [HDR] _LavaTopLightColor8("Top Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaTopLightHeight8("Top Light Height", Float) = 1.0
        [HDR] _LavaBottomLightColor8("Bottom Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaBottomLightHeight8("Bottom Light Height", Float) = -1.0
        [PowerSlider(2.72)] _LavaLightFalloff8("Light Falloff Scale", Range(0.0, 100.0)) = 1.0
        _LavaFlowDirection8("Flow Direction", Vector) = (0.0, 1.0, 0.0, 0.0)

        //Subregion 9
        [PowerSlider(2.72)] _LavaScale9("Scale", Range(0.001, 2.0)) = 0.5
        _LavaTopReservoirHeight9("Top Reservoir Height", Float) = 1.0
        _LavaBottomReservoirHeight9("Bottom Reservoir Height", Float) = -1.0
        _LavaCoreColor9("Lava Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaEdgeColor9("Lava Edge Color", Color) = (0.75, 0.75, 0.75, 1.0)
        [PowerSlider(2.72)] _LavaColorThicknessScale9("Lave Color Thickness Scale", Range(0.0, 30.0)) = 5.0
        _LavaWaterHazeColor9("Water Haze Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterHazeStrength9("Water Haze Density", Range(0.0, 50.0)) = 0.0
        _LavaWaterTintColor9("Water Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterTintStrength9("Water Tint Strength", Range(1.0, 100.0)) = 1.0
        [HDR] _LavaTopLightColor9("Top Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaTopLightHeight9("Top Light Height", Float) = 1.0
        [HDR] _LavaBottomLightColor9("Bottom Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaBottomLightHeight9("Bottom Light Height", Float) = -1.0
        [PowerSlider(2.72)] _LavaLightFalloff9("Light Falloff Scale", Range(0.0, 100.0)) = 1.0
        _LavaFlowDirection9("Flow Direction", Vector) = (0.0, 1.0, 0.0, 0.0)

        //Subregion 10
        [PowerSlider(2.72)] _LavaScale10("Scale", Range(0.001, 2.0)) = 0.5
        _LavaTopReservoirHeight10("Top Reservoir Height", Float) = 1.0
        _LavaBottomReservoirHeight10("Bottom Reservoir Height", Float) = -1.0
        _LavaCoreColor10("Lava Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaEdgeColor10("Lava Edge Color", Color) = (0.75, 0.75, 0.75, 1.0)
        [PowerSlider(2.72)] _LavaColorThicknessScale10("Lave Color Thickness Scale", Range(0.0, 30.0)) = 5.0
        _LavaWaterHazeColor10("Water Haze Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterHazeStrength10("Water Haze Density", Range(0.0, 50.0)) = 0.0
        _LavaWaterTintColor10("Water Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterTintStrength10("Water Tint Strength", Range(1.0, 100.0)) = 1.0
        [HDR] _LavaTopLightColor10("Top Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaTopLightHeight10("Top Light Height", Float) = 1.0
        [HDR] _LavaBottomLightColor10("Bottom Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaBottomLightHeight10("Bottom Light Height", Float) = -1.0
        [PowerSlider(2.72)] _LavaLightFalloff10("Light Falloff Scale", Range(0.0, 100.0)) = 1.0
        _LavaFlowDirection10("Flow Direction", Vector) = (0.0, 1.0, 0.0, 0.0)

        //Subregion 11
        [PowerSlider(2.72)] _LavaScale11("Scale", Range(0.001, 2.0)) = 0.5
        _LavaTopReservoirHeight11("Top Reservoir Height", Float) = 1.0
        _LavaBottomReservoirHeight11("Bottom Reservoir Height", Float) = -1.0
        _LavaCoreColor11("Lava Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaEdgeColor11("Lava Edge Color", Color) = (0.75, 0.75, 0.75, 1.0)
        [PowerSlider(2.72)] _LavaColorThicknessScale11("Lave Color Thickness Scale", Range(0.0, 30.0)) = 5.0
        _LavaWaterHazeColor11("Water Haze Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterHazeStrength11("Water Haze Density", Range(0.0, 50.0)) = 0.0
        _LavaWaterTintColor11("Water Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterTintStrength11("Water Tint Strength", Range(1.0, 100.0)) = 1.0
        [HDR] _LavaTopLightColor11("Top Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaTopLightHeight11("Top Light Height", Float) = 1.0
        [HDR] _LavaBottomLightColor11("Bottom Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaBottomLightHeight11("Bottom Light Height", Float) = -1.0
        [PowerSlider(2.72)] _LavaLightFalloff11("Light Falloff Scale", Range(0.0, 100.0)) = 1.0
        _LavaFlowDirection11("Flow Direction", Vector) = (0.0, 1.0, 0.0, 0.0)

        //Subregion 12
        [PowerSlider(2.72)] _LavaScale12("Scale", Range(0.001, 2.0)) = 0.5
        _LavaTopReservoirHeight12("Top Reservoir Height", Float) = 1.0
        _LavaBottomReservoirHeight12("Bottom Reservoir Height", Float) = -1.0
        _LavaCoreColor12("Lava Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaEdgeColor12("Lava Edge Color", Color) = (0.75, 0.75, 0.75, 1.0)
        [PowerSlider(2.72)] _LavaColorThicknessScale12("Lave Color Thickness Scale", Range(0.0, 30.0)) = 5.0
        _LavaWaterHazeColor12("Water Haze Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterHazeStrength12("Water Haze Density", Range(0.0, 50.0)) = 0.0
        _LavaWaterTintColor12("Water Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterTintStrength12("Water Tint Strength", Range(1.0, 100.0)) = 1.0
        [HDR] _LavaTopLightColor12("Top Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaTopLightHeight12("Top Light Height", Float) = 1.0
        [HDR] _LavaBottomLightColor12("Bottom Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaBottomLightHeight12("Bottom Light Height", Float) = -1.0
        [PowerSlider(2.72)] _LavaLightFalloff12("Light Falloff Scale", Range(0.0, 100.0)) = 1.0
        _LavaFlowDirection12("Flow Direction", Vector) = (0.0, 1.0, 0.0, 0.0)

        //Subregion 13
        [PowerSlider(2.72)] _LavaScale13("Scale", Range(0.001, 2.0)) = 0.5
        _LavaTopReservoirHeight13("Top Reservoir Height", Float) = 1.0
        _LavaBottomReservoirHeight13("Bottom Reservoir Height", Float) = -1.0
        _LavaCoreColor13("Lava Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaEdgeColor13("Lava Edge Color", Color) = (0.75, 0.75, 0.75, 1.0)
        [PowerSlider(2.72)] _LavaColorThicknessScale13("Lave Color Thickness Scale", Range(0.0, 30.0)) = 5.0
        _LavaWaterHazeColor13("Water Haze Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterHazeStrength13("Water Haze Density", Range(0.0, 50.0)) = 0.0
        _LavaWaterTintColor13("Water Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterTintStrength13("Water Tint Strength", Range(1.0, 100.0)) = 1.0
        [HDR] _LavaTopLightColor13("Top Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaTopLightHeight13("Top Light Height", Float) = 1.0
        [HDR] _LavaBottomLightColor13("Bottom Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaBottomLightHeight13("Bottom Light Height", Float) = -1.0
        [PowerSlider(2.72)] _LavaLightFalloff13("Light Falloff Scale", Range(0.0, 100.0)) = 1.0
        _LavaFlowDirection13("Flow Direction", Vector) = (0.0, 1.0, 0.0, 0.0)

        //Subregion 14
        [PowerSlider(2.72)] _LavaScale14("Scale", Range(0.001, 2.0)) = 0.5
        _LavaTopReservoirHeight14("Top Reservoir Height", Float) = 1.0
        _LavaBottomReservoirHeight14("Bottom Reservoir Height", Float) = -1.0
        _LavaCoreColor14("Lava Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaEdgeColor14("Lava Edge Color", Color) = (0.75, 0.75, 0.75, 1.0)
        [PowerSlider(2.72)] _LavaColorThicknessScale14("Lave Color Thickness Scale", Range(0.0, 30.0)) = 5.0
        _LavaWaterHazeColor14("Water Haze Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterHazeStrength14("Water Haze Density", Range(0.0, 50.0)) = 0.0
        _LavaWaterTintColor14("Water Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterTintStrength14("Water Tint Strength", Range(1.0, 100.0)) = 1.0
        [HDR] _LavaTopLightColor14("Top Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaTopLightHeight14("Top Light Height", Float) = 1.0
        [HDR] _LavaBottomLightColor14("Bottom Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaBottomLightHeight14("Bottom Light Height", Float) = -1.0
        [PowerSlider(2.72)] _LavaLightFalloff14("Light Falloff Scale", Range(0.0, 100.0)) = 1.0
        _LavaFlowDirection14("Flow Direction", Vector) = (0.0, 1.0, 0.0, 0.0)

        //Subregion 15
        [PowerSlider(2.72)] _LavaScale15("Scale", Range(0.001, 2.0)) = 0.5
        _LavaTopReservoirHeight15("Top Reservoir Height", Float) = 1.0
        _LavaBottomReservoirHeight15("Bottom Reservoir Height", Float) = -1.0
        _LavaCoreColor15("Lava Core Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaEdgeColor15("Lava Edge Color", Color) = (0.75, 0.75, 0.75, 1.0)
        [PowerSlider(2.72)] _LavaColorThicknessScale15("Lave Color Thickness Scale", Range(0.0, 30.0)) = 5.0
        _LavaWaterHazeColor15("Water Haze Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterHazeStrength15("Water Haze Density", Range(0.0, 50.0)) = 0.0
        _LavaWaterTintColor15("Water Tint", Color) = (1.0, 1.0, 1.0, 1.0)
        [PowerSlider(2.72)] _LavaWaterTintStrength15("Water Tint Strength", Range(1.0, 100.0)) = 1.0
        [HDR] _LavaTopLightColor15("Top Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaTopLightHeight15("Top Light Height", Float) = 1.0
        [HDR] _LavaBottomLightColor15("Bottom Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _LavaBottomLightHeight15("Bottom Light Height", Float) = -1.0
        [PowerSlider(2.72)] _LavaLightFalloff15("Light Falloff Scale", Range(0.0, 100.0)) = 1.0
        _LavaFlowDirection15("Flow Direction", Vector) = (0.0, 1.0, 0.0, 0.0)

        //Mesh Data
        [NoScaleOffset] _VertexBindPositions("Bind Positions Texture", 2D) = "black" {}
        [NoScaleOffset] _VertexBindNormals("Bind Normals Texture", 2D) = "black" {}
        [NoScaleOffset] _VertexBindTangents("Bind Tangents Texture", 2D) = "black" {}
        _WorldRecale("World Rescale", Float) = 1.0

        //Volume Data
        [noscaleoffset] _SDFTexture("SDF Texture", 3D) = "black" {}
        _SDFPixelSize("SDF Pixel Size", Float) = 1.0
        _SDFLowerCorner("SDF Lower Corner", Vector) = (0.0, 0.0, 0.0, 0.0)
        _SDFSize("SDF Size", Vector) = (1.0, 1.0, 1.0, 0.0)
        _MinThickness("Min Thickness", Range(0.0, 50.0)) = 0.01

        //Advanced Rendering Options, used by the editor
        [IntRange] _LavaSubregionCount("Subregion Count", Range(1, 16)) = 1.0
        [Toggle] _Lighting_Toggle("Surface Lighting", Float) = 1.0
        [Toggle] _Transparency_Toggle("Transparent Background", Float) = 1.0
        [Toggle] _DepthIntersection_Toggle("Lava Depth Intersection", Float) = 1.0
        [Toggle] _WriteDepth_Toggle("Write Depth", Float) = 1.0
        [IntRange] _DepthOffset("Depth Offset", Range(-1.0, 1.0)) = 0.0
        
        [HideInInspector] _ZWrite("ZWrite", Float) = 1.0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" }
        Offset [_DepthOffset], [_DepthOffset]

        GrabPass
        {
            Tags { "LightMode" = "Always" } //this pass gets disabled by the ShaderGUI when transparency is disabled

            "_LavaLampGrabTexture" //only do a single grab pass for all lava lamps for the sake of performance
        }

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            ZWrite [_ZWrite]
            ZTest LEqual
            Blend Off
            
            HLSLPROGRAM
            
            #pragma target 5.0
            #pragma multi_compile_fwdbase novertexlight nolightmap nodynlightmap nodirlightmap
            #pragma multi_compile_fog 
            #pragma multi_compile_instancing
            
            #pragma shader_feature_local LAVA_LAMP_USE_TRANSPARENCY
            #pragma shader_feature_local LAVA_LAMP_DEPTH_INTERSECTION
            #pragma shader_feature_local LAVA_LAMP_USE_LIGHTING
            #pragma shader_feature_local __ LAVA_LAMP_SUBREGION_COUNT_2 LAVA_LAMP_SUBREGION_COUNT_3 LAVA_LAMP_SUBREGION_COUNT_4 LAVA_LAMP_SUBREGION_COUNT_5 LAVA_LAMP_SUBREGION_COUNT_6 LAVA_LAMP_SUBREGION_COUNT_7 LAVA_LAMP_SUBREGION_COUNT_8 LAVA_LAMP_SUBREGION_COUNT_9 LAVA_LAMP_SUBREGION_COUNT_10 LAVA_LAMP_SUBREGION_COUNT_11 LAVA_LAMP_SUBREGION_COUNT_12 LAVA_LAMP_SUBREGION_COUNT_13 LAVA_LAMP_SUBREGION_COUNT_14 LAVA_LAMP_SUBREGION_COUNT_15 LAVA_LAMP_SUBREGION_COUNT_16
            
            #pragma vertex LavaLampBaseVertexShader
            #pragma fragment LavaLampBasePixelShader
            
            #include "Helpers/LavaLampCommon.hlsl"
            
            ENDHLSL
        }

        Pass
        {
            //this pass gets disabled by the ShaderGUI when lighting is disabled
            Tags { "LightMode" = "ForwardAdd" }

            ZWrite Off
            ZTest LEqual
            Blend One One
            Fog { Color(0,0,0,0) } // in additive pass fog should be black
            
            HLSLPROGRAM
            
            #pragma target 5.0
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog 
            #pragma multi_compile_instancing
            
            #pragma shader_feature_local __ LAVA_LAMP_SUBREGION_COUNT_2 LAVA_LAMP_SUBREGION_COUNT_3 LAVA_LAMP_SUBREGION_COUNT_4 LAVA_LAMP_SUBREGION_COUNT_5 LAVA_LAMP_SUBREGION_COUNT_6 LAVA_LAMP_SUBREGION_COUNT_7 LAVA_LAMP_SUBREGION_COUNT_8 LAVA_LAMP_SUBREGION_COUNT_9 LAVA_LAMP_SUBREGION_COUNT_10 LAVA_LAMP_SUBREGION_COUNT_11 LAVA_LAMP_SUBREGION_COUNT_12 LAVA_LAMP_SUBREGION_COUNT_13 LAVA_LAMP_SUBREGION_COUNT_14 LAVA_LAMP_SUBREGION_COUNT_15 LAVA_LAMP_SUBREGION_COUNT_16
            
            #pragma vertex LavaLampLightingVertexShader
            #pragma fragment LavaLampLightingPixelShader
            
            #include "Helpers/LavaLampCommon.hlsl"
            
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM

            #pragma target 5.0
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing

            #pragma shader_feature_local __ LAVA_LAMP_SUBREGION_COUNT_2 LAVA_LAMP_SUBREGION_COUNT_3 LAVA_LAMP_SUBREGION_COUNT_4 LAVA_LAMP_SUBREGION_COUNT_5 LAVA_LAMP_SUBREGION_COUNT_6 LAVA_LAMP_SUBREGION_COUNT_7 LAVA_LAMP_SUBREGION_COUNT_8 LAVA_LAMP_SUBREGION_COUNT_9 LAVA_LAMP_SUBREGION_COUNT_10 LAVA_LAMP_SUBREGION_COUNT_11 LAVA_LAMP_SUBREGION_COUNT_12 LAVA_LAMP_SUBREGION_COUNT_13 LAVA_LAMP_SUBREGION_COUNT_14 LAVA_LAMP_SUBREGION_COUNT_15 LAVA_LAMP_SUBREGION_COUNT_16

            #pragma vertex LavaLampShadowVertexShader
            #pragma fragment LavaLampShadowPixelShader
            
            #include "Helpers/LavaLampCommon.hlsl"

            ENDHLSL
        }
    }

    CustomEditor "LavaLampShaderGUI"
}
