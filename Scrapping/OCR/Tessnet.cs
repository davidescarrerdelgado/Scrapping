using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tessnet2;

namespace OCR
{
    public class  Tessnet
    {
        public string detect(string sPathImg)
        {

            try
            {
                var image = new Bitmap(sPathImg);
                var ocr = new Tesseract();
                ocr.SetVariable("tessedit_char_whitelist", "0123456789"); // If digit only
                //@"C:\OCRTest\tessdata" contains the language package, without this the method crash and app breaks
                ocr.Init(@"D:\GestionVacacional\Scrapping\Scrapping\Tessdata", "eng", true);
                var result = ocr.DoOCR(image, Rectangle.Empty);
                foreach (Word word in result)
                return word.Text;
            }
            catch (Exception)
            {
                return "";
            }

            return "";

        }
    }
}
