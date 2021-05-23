using GlassAssistant.Constants;
using GlassAssistant.DataStructures;
using GlassAssistant.Enums;
using GlassAssistant.WindowMain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GlassAssistant.MachineLearning
{
    internal static class InputDataValidation
    {
        private static double maxUnitWidth = 3; // [m]
        private static double minUnitWidth = 0.4; // [m]
        private static double maxUnitHeight = 5; // [m]
        private static double minUnitHeight = 0.4; // [m]
        private static double minSingleLayerThickness = 0.004; // [m]
        private static double maxSingleLayerThickness = 0.012; // [m]
        private static double maxLineLoadMagnitude = 1500; // [N/m]
        private static double maxPointLoadMagnitude = 1500; // [N]
        private static double pointLoadSize = 0.1; // [m]

        public static List<FormInputValidationMessage> MachineLearningSpecificValidation(FormInputData inputData)
        {
            var validationEntries = new List<FormInputValidationEntry>()
            {
                new FormInputValidationEntry(nameof(FormInputData.Width), ComparisonType.Lesser, minUnitWidth,
                    ErrorLevel.Red,
                    $"Glass unit width is smaller than  {minUnitWidth}m."),

                new FormInputValidationEntry(nameof(FormInputData.Height), ComparisonType.Lesser, minUnitHeight,
                    ErrorLevel.Red,
                    $"Glass unit height is smaller than  {minUnitHeight}m."),

                new FormInputValidationEntry(nameof(FormInputData.Width), ComparisonType.Greater, maxUnitWidth,
                    ErrorLevel.Red,
                    $"Manufacturer \"Stiklu centrs\" produces maximum unit width {maxUnitWidth}m."),

                new FormInputValidationEntry(nameof(FormInputData.Height), ComparisonType.Greater, maxUnitHeight,
                    ErrorLevel.Red,
                    $"Manufacturer \"Stiklu centrs\" produces maximum unit height {maxUnitHeight}m."),

                new FormInputValidationEntry(nameof(FormInputData.ExternalLayer1Thickness), ComparisonType.Lesser,
                    minSingleLayerThickness, ErrorLevel.Red,
                    $"One of External pane layers has thickness smaller than {minSingleLayerThickness * Conversion.MtoMm}mm"),

                new FormInputValidationEntry(nameof(FormInputData.ExternalLayer2Thickness), ComparisonType.Lesser,
                    minSingleLayerThickness, ErrorLevel.Red,
                    $"One of External pane layers has thickness smaller than {minSingleLayerThickness * Conversion.MtoMm}mm"),

                new FormInputValidationEntry(nameof(FormInputData.ExternalLayer1Thickness), ComparisonType.Greater,
                    maxSingleLayerThickness, ErrorLevel.Red,
                    $"One of External pane layers has thickness larger than {maxSingleLayerThickness * Conversion.MtoMm}mm"),

                new FormInputValidationEntry(nameof(FormInputData.ExternalLayer2Thickness), ComparisonType.Greater,
                    maxSingleLayerThickness, ErrorLevel.Red,
                    $"One of External pane layers has thickness larger than {maxSingleLayerThickness * Conversion.MtoMm}mm"),

                new FormInputValidationEntry(nameof(FormInputData.InternalLayer1Thickness), ComparisonType.Lesser,
                    minSingleLayerThickness, ErrorLevel.Red,
                    $"One of Internal pane layers has thickness smaller than {minSingleLayerThickness * Conversion.MtoMm}mm"),

                new FormInputValidationEntry(nameof(FormInputData.InternalLayer2Thickness), ComparisonType.Lesser,
                    minSingleLayerThickness, ErrorLevel.Red,
                    $"One of Internal pane layers has thickness smaller than {minSingleLayerThickness * Conversion.MtoMm}mm"),

                new FormInputValidationEntry(nameof(FormInputData.InternalLayer1Thickness), ComparisonType.Greater,
                    maxSingleLayerThickness, ErrorLevel.Red,
                    $"One of Internal pane layers has thickness larger than {maxSingleLayerThickness * Conversion.MtoMm}mm"),

                new FormInputValidationEntry(nameof(FormInputData.InternalLayer2Thickness), ComparisonType.Greater,
                    maxSingleLayerThickness, ErrorLevel.Red,
                    $"One of Internal pane layers has thickness larger than {maxSingleLayerThickness * Conversion.MtoMm}mm"),

                new FormInputValidationEntry(nameof(FormInputData.MiddleLayer1Thickness), ComparisonType.Lesser,
                    minSingleLayerThickness, ErrorLevel.Red,
                    $"One of Middle pane layers has thickness smaller than {minSingleLayerThickness * Conversion.MtoMm}mm"),

                new FormInputValidationEntry(nameof(FormInputData.MiddleLayer2Thickness), ComparisonType.Lesser,
                    minSingleLayerThickness, ErrorLevel.Red,
                    $"One of Middle pane layers has thickness smaller than {minSingleLayerThickness * Conversion.MtoMm}mm"),

                new FormInputValidationEntry(nameof(FormInputData.MiddleLayer1Thickness), ComparisonType.Greater,
                    maxSingleLayerThickness, ErrorLevel.Red,
                    $"One of Middle pane layers has thickness larger than {maxSingleLayerThickness * Conversion.MtoMm}mm"),

                new FormInputValidationEntry(nameof(FormInputData.MiddleLayer2Thickness), ComparisonType.Greater,
                    maxSingleLayerThickness, ErrorLevel.Red,
                    $"One of Middle pane layers has thickness larger than {maxSingleLayerThickness * Conversion.MtoMm}mm"),

                new FormInputValidationEntry(nameof(FormInputData.LineLoadMagnitude), ComparisonType.Greater,
                    maxLineLoadMagnitude, ErrorLevel.Red,
                    $"Line load is larger than {maxLineLoadMagnitude * Conversion.NtoKn}kN/m"),

                new FormInputValidationEntry(nameof(FormInputData.PointLoadMagnitude), ComparisonType.Greater,
                    maxPointLoadMagnitude, ErrorLevel.Red,
                    $"Point load magnitude is larger than {maxPointLoadMagnitude * Conversion.NtoKn}kN"),
            };
            var validationMessages = validationEntries.Where(x => x.IsEntryInvalid(inputData))
                .Select(x => x.GetMessage()).ToList();

            if (inputData.UnitType == GlassUnitType.Balustrade)
            {
                var msg = "Machine learning can't be used for calculation of balustrades.";
                validationMessages.Add(new FormInputValidationMessage(msg, ErrorLevel.Red));
            }

            if (Math.Abs(inputData.PointLoadSize - pointLoadSize) > GlobalConstants.FloatingPointTolerance
                && inputData.PointLoadSize != 0)
            {
                var msg = $"Machine learning only processes point load with size {pointLoadSize}m ";
                validationMessages.Add(new FormInputValidationMessage(msg, ErrorLevel.Red));
            }

            if (Math.Abs(inputData.PointLoadHeight - (inputData.Height / 2)) > GlobalConstants.FloatingPointTolerance
                && inputData.PointLoadHeight != 0)
            {
                var msg = "Point load must be located in the centre of unit for machine learning predictions!";
                validationMessages.Add(new FormInputValidationMessage(msg, ErrorLevel.Red));
            }

            return validationMessages;
        }

        public static void ThrowValidationErrors(List<FormInputValidationMessage> validationErrors)
        {
            var intro = "The Machine learning process could not be run because following parameters are outside " +
                        "of supported range:";
            var messages = validationErrors.Select(x => x.Message).ToList();
            var allOutputTextList = messages.Prepend(intro);

            var fullMessage = string.Join("\n", allOutputTextList);
            Common.CommonMethods.ShowTopMostMessageBox(fullMessage);
        }
    }
}