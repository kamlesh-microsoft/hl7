using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using CommonUtils;
using NHapi.Base.Parser;
using NHapi.Model.V23.Datatype;
using NHapi.Model.V23.Message;

namespace ReceivingBinaryDataExample
{
    public class Program
    {
        private static readonly string OruR01MessageWithBase64EncodedPdfReportIncluded = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Test Files", "SaravananOruR01Message.hl7");
        private const string ExtractedPdfOutputDirectory = "C:\\HL7TestOutputs";

        public static void Main(string[] args)
        {
            try
            {
                // instantiate a PipeParser, which handles the "traditional or default encoding"
                var ourPipeParser = new PipeParser();
                // parse the string format message into a Java message object
                var hl7Message = ourPipeParser.Parse(File.ReadAllText(OruR01MessageWithBase64EncodedPdfReportIncluded, Encoding.UTF8));

                //cast to message to an ORU R01 message in order to get access to the message data
                var oruR01Message = hl7Message as ORU_R01;
                if (oruR01Message != null)
                {
                    // Display the updated HL7 message using Pipe delimited format
                    LogToDebugConsole("Parsed HL7 Message:");
                    LogToDebugConsole(ourPipeParser.Encode(hl7Message));

                    var encapsulatedPdfDataInBase64Format = ExtractEncapsulatedPdfDataInBase64Format(oruR01Message);

                    //if no encapsulated data was found, you can cease operation
                    if (encapsulatedPdfDataInBase64Format == null) return;

                    var extractedPdfByteData = GetBase64DecodedPdfByteData(encapsulatedPdfDataInBase64Format);

                    WriteExtractedPdfByteDataToFile(extractedPdfByteData);
                }
            }
            catch (Exception e)
            {
                LogToDebugConsole($"Error occured during Order Response PDF extraction operation -> {e.StackTrace}");
            }
        }

        private static ED ExtractEncapsulatedPdfDataInBase64Format(ORU_R01 oruR01Message)
        {
            //start retrieving the OBX segment data to get at the PDF report content
            LogToDebugConsole("Extracting message data from parsed message..");
            var ourOrderObservation = oruR01Message.GetRESPONSE().GetORDER_OBSERVATION();
            var observation = ourOrderObservation.GetOBSERVATION(0);
            var obxSegment = observation.OBX;

            var encapsulatedPdfDataInBase64Format = obxSegment.GetObservationValue(0).Data as ED;
            return encapsulatedPdfDataInBase64Format;
        }

        private static byte[] GetBase64DecodedPdfByteData(ED encapsulatedPdfDataInBase64Format)
        {
            var helper = new OurBase64Helper();

            LogToDebugConsole("Extracting PDF data stored in Base-64 encoded form from OBX-5..");
            var base64EncodedByteData = encapsulatedPdfDataInBase64Format.Data.Value;
            var extractedPdfByteData = helper.ConvertFromBase64String(base64EncodedByteData);
            return extractedPdfByteData;
        }

        private static void WriteExtractedPdfByteDataToFile(byte[] extractedPdfByteData)
        {
            LogToDebugConsole($"Creating output directory at '{ExtractedPdfOutputDirectory}'..");

            if (!Directory.Exists(ExtractedPdfOutputDirectory))
                Directory.CreateDirectory(ExtractedPdfOutputDirectory);

            var pdfOutputFile = Path.Combine(ExtractedPdfOutputDirectory, "ExtractedPdfReport.pdf");
            LogToDebugConsole(
                $"Writing the extracted PDF data to '{pdfOutputFile}'. You should be able to see the decoded PDF content..");
            File.WriteAllBytes(pdfOutputFile, extractedPdfByteData);

            LogToDebugConsole("Extraction operation was successfully completed..");
        }

        private static void LogToDebugConsole(string informationToLog)
        {
            Debug.WriteLine(informationToLog);
        }
    }
}
