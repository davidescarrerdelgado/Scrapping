using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Data;
using System.IO;
using OCR;
using System.Text.RegularExpressions;
using HtmlAgilityPack;


namespace Scrapping
{
    class SegundaMano
    {
        /*Configuraciones globales del scrapping*/
        #region Global Configuration
        public int iWaitTime = 0;
        public iMacros.Status status;
        public iMacros.AppClass app;
        public HtmlDocument htmlDoc;
        public int globalCount;
        static int globalCountMax;
        #endregion

        //Conexion a la base de datos via Entity Framework 6.0
        #region Conexion Entity Framework
        public ScrappingEntities context = new ScrappingEntities();
        #endregion

        /*Constructor SegundaMano*/
        #region Constructor Clase
        public SegundaMano(){
            
            this.iWaitTime = 1; //Tiempo de espera para scrapear cada pagina
            globalCountMax = 25; //Cantidade de iteraciones para reiniciar el imacros
            this.globalCount = globalCountMax;
            this.context.Configuration.AutoDetectChangesEnabled = false;
            this.context.Configuration.ValidateOnSaveEnabled = false;
            this.htmlDoc = new HtmlDocument();

            //Declaracion de la Instancia del imacros
            #region Instancia Imacros
            iMacros.Status status;
            app = new iMacros.AppClass();
            status = app.iimInit("-tray", false);
            #endregion

        }
        #endregion

        /*Destructor SegundaMano*/
        #region Destructor SegundaMano
        ~SegundaMano()
        {
            
            try
            {
                app.iimClose();
            }
            catch
            {
            }

        }
        #endregion

        /*Gestion de Memoria*/
        #region Metodo responsable por gestionar la memoria limitada del imacros
        public void controlMemoriaImacros(){
            if (globalCount == 0)
            {
                app.iimClose();
                app = new iMacros.AppClass();
                status = app.iimInit("-tray", false);
                string sScript = "";
                sScript = "CODE:" + Environment.NewLine;
                sScript += "CLEAR" + Environment.NewLine;
                sScript += "VERSION BUILD=9002379" + Environment.NewLine;
                sScript += "TAB T=1" + Environment.NewLine;
                sScript += "TAB CLOSEALLOTHERS" + Environment.NewLine;
                sScript += "FILTER TYPE=IMAGES STATUS=ON " + Environment.NewLine;
                app.iimPlay(sScript);
                globalCount = globalCountMax;
            }
            else
            {
                globalCount--;
            }
        }
        #endregion

        public void ScrapearListaPropriedas()
        {

            //Variables de control del scrapping
            #region Declaracion Variables de Control
            Boolean bEndOfPage = false;
            Boolean bEndOfScrapping = false;
            int i = 1;
            string sUrl = "";
            string sUrlNextPage = "";
            string sScript = "";
            string sPublicacion = "";
            #endregion


            /*Lista de paginas a scrapear*/
            using (var db = new ScrappingEntities())
            {
                foreach (var Enlace in db.Enlace.Where((s => s.web == "segundamano")).OrderBy(s => s.id_enlace))
                {

                    //Restarting variables de control
                    bEndOfPage = false;
                    bEndOfScrapping = false;
                    i = 1;
                    sUrl = "";
                    sPublicacion = "";
                    sUrlNextPage = "";
                    sScript = "";

                    //Preparacion del sScript de scrapping
                    sScript = "CODE:" + Environment.NewLine;
                    sScript += "CLEAR" + Environment.NewLine;
                    sScript += "VERSION BUILD=9002379" + Environment.NewLine;
                    sScript += "TAB T=1" + Environment.NewLine;
                    sScript += "TAB CLOSEALLOTHERS" + Environment.NewLine;
                    sScript += "URL GOTO=" + Enlace.url + Environment.NewLine;
                    sScript += "WAIT SECONDS=" + iWaitTime + Environment.NewLine;

                    //Llamada al imacros
                    app.iimPlay(sScript);

                    //Detectamos si el scrapping ha terminado
                    while (!bEndOfScrapping)
                    {

                        //Detectamos si la pagina ha terminado
                        while (!bEndOfPage)
                        {
                            //Looping por cada uno de los elementos encontrados
                            sScript = "CODE:TAG POS=" + i + " TYPE=a ATTR=class:dateLink EXTRACT=HREF";
                            app.iimPlay(sScript);
                            sUrl = app.iimGetLastExtract();
                            sUrl = sUrl.Replace("[EXTRACT]", "");

                            if ((sUrl.IndexOf("#EANF#") >= 0) || (sUrl.IndexOf("NODATA") >= 0))
                            {
                                bEndOfPage = true;
                            }
                            else
                            {

                                //Capturando la fecha de publicacion
                                sScript = "CODE:TAG POS=" + i + " TYPE=a ATTR=class:dateLink EXTRACT=TXT";
                                app.iimPlay(sScript);
                                sPublicacion = app.iimGetLastExtract();
                                sPublicacion = sPublicacion.Replace("[EXTRACT]", "");


                                //Configurando insert a la base de datos.
                                Enlace_Hijo_Validate Validate = new Enlace_Hijo_Validate();
                                Validate.publish_date = formatearFecha(sPublicacion);
                                Validate.url = sUrl;
                                Validate.catched_date = DateTime.Now;
                                Validate.id_enlace = Enlace.id_enlace;

                                //Guardando nueva liena
                                context.Enlace_Hijo_Validate.Add(Validate);
                                context.SaveChanges();
                                i++;
                            }

                        }
                        Console.WriteLine("End of page");

                        //Valida si hay una nueva pagina para recuperar enlaces
                        sScript = "CODE:TAG POS=1 TYPE=a ATTR=class:\"paginationLink paginationNextLink\" EXTRACT=HREF";
                        app.iimPlay(sScript);
                        sUrlNextPage = app.iimGetLastExtract();
                        sUrlNextPage = sUrlNextPage.Replace("[EXTRACT]", "");
                        if ((sUrlNextPage.IndexOf("#EANF#") >= 0) || (sUrlNextPage.IndexOf("NODATA") >= 0))
                        {
                            bEndOfScrapping = true;
                            bEndOfPage = true;
                            Console.WriteLine("Enf of Enlace: " + Enlace.url);

                            /*Lanzamos el proceso que valida las nuevas propriedades*/
                            context.Database.ExecuteSqlCommand("SP_ENLACE_HIJO_VALIDATE @id_enlace = {0}", Enlace.id_enlace);

                        }
                        else
                        {

                            Console.WriteLine(sUrlNextPage);
                            //Saltamos a la seguinte pagina
                            i = 1;
                            bEndOfPage = false;
                            bEndOfScrapping = false;
                            sScript = "CODE:URL GOTO=" + sUrlNextPage + Environment.NewLine;
                            app.iimPlay(sScript);

                            sScript = "CODE:WAIT SECONDS=" + iWaitTime + Environment.NewLine;
                            app.iimPlay(sScript);
                        }
                    }


                }
            }


            /*Cerramos el imacros*/
            app.iimClose();
        }

        public void ScrapearPropriedadesMasivo(int id_enlace)
        {
             /*Lista de paginas a scrapear*/
            using (var db = new ScrappingEntities())
            {
                foreach (var Enlace in db.Enlace_Hijo.Where((s => s.id_enlace == id_enlace && s.scrapped_date == null && s.inactive_date == null)).OrderBy(s => s.id_enlace))
                {
                    this.ScrapearPropriedade(Enlace.id_enlace_hijo);
                    this.controlMemoriaImacros();
                }
            }
        }

        public void ScrapearPropriedade(int id_enlace_hijo)
        {
           
            #region 1. Declaracion Variables de Control
            var Hijo = context.Enlace_Hijo.First(a => a.id_enlace_hijo == id_enlace_hijo);
            
            //Lector optico para conversion de las imagenes en texto
            //Tessnet Tessnet = new Tessnet();

            string sScript = "";
            string sTxt = "";

            int iThumbs = 0;
            Boolean bEndOfThumbs = false;

            int iFeatures = 1;
            Boolean bEnfOfFeatures = false;

            int iTelefono = 1;
            Boolean bEnfOfTelefono = false;
            string sTelefono = "";

            int iDescripcion = 1;
            Boolean bEnfOfDescripcion = false;
            string sDescripcion = "";
            string sDescripcionValue = "";
            #endregion

            #region 2. Preparacion de las tablas para insercion
            //Configurando inserts a la base de datos.
            Enlace_Hijo_Detalle Detalle = new Enlace_Hijo_Detalle();
            Detalle.id_enlace_hijo = id_enlace_hijo;

            //Limpiamos datos de las caracteristicas anteriores a la misma propriedade
            this.DeletarCaracteristica(id_enlace_hijo);
            this.DeletarDetalle(id_enlace_hijo);
            #endregion

            #region 3. Configuracion del script imacros
            //Preparacion del sScript de scrapping
            sScript = "CODE:" + Environment.NewLine;
            sScript += "CLEAR" + Environment.NewLine;
            sScript += "VERSION BUILD=9002379" + Environment.NewLine;
            sScript += "TAB T=1" + Environment.NewLine;
            sScript += "TAB CLOSEALLOTHERS" + Environment.NewLine;
            sScript += "FILTER TYPE=IMAGES STATUS=ON " + Environment.NewLine;
            sScript += "URL GOTO="+Hijo.url + Environment.NewLine;
            sScript += "WAIT SECONDS=" + iWaitTime + Environment.NewLine;
            status = app.iimPlay(sScript);
            if (status.ToString() != "sOk"){
                erroScrapearPropriedade(id_enlace_hijo, status.ToString());
                this.globalCount = 0;
                controlMemoriaImacros();
                return;
            }
            #endregion

            #region 4. Validacion si el anuncio sigue disponible
            sScript = "CODE:TAG POS=1 TYPE=h2 ATTR=class:\"titleConfirmPayNotNeed\" EXTRACT=TXT" + Environment.NewLine;
            app.iimPlay(sScript);
            if (this.cleanString(app.iimGetLastExtract()) == "el anuncio que buscas no está en segundamano.es")
            {
                var disponible = context.Enlace_Hijo.SingleOrDefault(b => b.id_enlace_hijo == id_enlace_hijo);
                if (disponible != null)
                {
                    context.Entry(disponible).State = System.Data.Entity.EntityState.Modified;
                    disponible.scrapped_date = DateTime.Now;
                    disponible.inactive_date = DateTime.Now;
                    disponible.error_handler = "OK";
                    context.SaveChanges();
                    Console.WriteLine(id_enlace_hijo + " -> el anuncio que buscas no está en segundamano.es");
                    return;
                }
            }
            #endregion

            #region EXTRACT -> Nombre del piso
            sScript = "CODE:TAG POS=1 TYPE=h1 ATTR=class:\"productTitle\" EXTRACT=TXT" + Environment.NewLine;
            app.iimPlay(sScript);
            try { Detalle.nombre = this.cleanString(app.iimGetLastExtract()); } catch { Detalle.nombre = null; }
            Console.WriteLine(id_enlace_hijo + " -> "+ Detalle.nombre + ": DONE");
            #endregion

            #region EXTRACT -> Precio
            sScript = "CODE:TAG POS=1 TYPE=span ATTR=class:\"price\" EXTRACT=TXT" + Environment.NewLine;
            app.iimPlay(sScript);
            try { Detalle.precio = Convert.ToInt16(Regex.Replace(this.cleanString(app.iimGetLastExtract()), "[^0-9.]", "")); }catch { Detalle.precio = null; }
            #endregion

            #region EXTRACT -> Descripcion
            sScript = "CODE:TAG POS=1 TYPE=p ATTR=id:\"descriptionText\" EXTRACT=TXT" + Environment.NewLine;
            app.iimPlay(sScript);
            try { Detalle.descripcion = this.cleanString(app.iimGetLastExtract()); } catch { Detalle.descripcion = null; }
            #endregion

            #region EXTRACT -> Cuantidade de veces visto
            sScript = "CODE:TAG POS=1 TYPE=p ATTR=class:\"TimesSeen\" EXTRACT=TXT" + Environment.NewLine;
            app.iimPlay(sScript);
            try { Detalle.visitas = Convert.ToInt16(Regex.Replace(this.cleanString(app.iimGetLastExtract()), "[^0-9.]", "")); }catch { Detalle.visitas = null; }
            #endregion

            #region EXTRACT -> Nombre contacto
            sScript = "CODE:TAG POS=1 TYPE=span ATTR=class:\"Cname\" EXTRACT=TXT" + Environment.NewLine;
            app.iimPlay(sScript);
            try { Detalle.nombre_contacto = this.cleanString(app.iimGetLastExtract()); }catch { Detalle.nombre_contacto = null; }
            #endregion

            #region EXTRACT -> Tiene whatsapp
            sScript = "CODE:TAG POS=1 TYPE=div ATTR=class:\"whatsapp\" EXTRACT=TXT" + Environment.NewLine;
            app.iimPlay(sScript);
            try { Detalle.usuarioWhatsapp = this.cleanString(app.iimGetLastExtract()); } catch { Detalle.usuarioWhatsapp = null; }
            #endregion

            #region EXTRACT -> Bread crumbie con las categorias asociadas al piso - HTMLAGILEPACK
            try
            { 
            sScript = "CODE:TAG POS=1 TYPE=p ATTR=class:\"breadcrumbs\" EXTRACT=HTM" + Environment.NewLine;
            app.iimPlay(sScript);
            sTxt = this.cleanString(app.iimGetLastExtract());
            
            //HTMLAGILEPACK para parsear html
            htmlDoc.LoadHtml(sTxt);
            var ahrefs = htmlDoc.DocumentNode.SelectNodes("//a");
            foreach (var input in ahrefs)
            {
                //Console.WriteLine(input.InnerText);
                Enlace_Hijo_Caracteristica Caracteristica = new Enlace_Hijo_Caracteristica();
                Caracteristica.id_enlace_hijo = id_enlace_hijo;
                Caracteristica.caracteristica_validate = "CATEGORIA";
                Caracteristica.tipo_validate = 2;
                Caracteristica.value = input.InnerText;
                Caracteristica.sysdate = DateTime.Now;
                context.Enlace_Hijo_Caracteristica.Add(Caracteristica);
            }
            sTxt = "";
            }
            catch (Exception)
            {
                Console.WriteLine("Error BreadCrumbie:" + id_enlace_hijo);
            }
            #endregion

            #region EXTRACT -> Descripcion basica del piso - HTMLAGILEPACK
            try
            { 
            sScript = "CODE:TAG POS=1 TYPE=dl ATTR=class:\"descriptionFeatures descriptionRight\" EXTRACT=HTM" + Environment.NewLine;
            app.iimPlay(sScript);
            sTxt = this.cleanString(app.iimGetLastExtract());
            
            //HTMLAGILEPACK para parsear html
            htmlDoc.LoadHtml(sTxt);

            List<string> listaDT = new List<string>();
            var dts = htmlDoc.DocumentNode.SelectNodes("//dt");
            foreach (var input in dts)
            {
                listaDT.Add(input.InnerText);
            }

            List<string> listaDD = new List<string>();
            var dds = htmlDoc.DocumentNode.SelectNodes("//dd");
            foreach (var input in dds)
            {
                String abc = String.Empty;
                if (!input.Attributes.Contains(@"class"))
                {
                    listaDD.Add(input.InnerText);
                }
            }


            for (int i = 0; i < listaDT.Count; i++) 
            {
                Enlace_Hijo_Caracteristica Caracteristica = new Enlace_Hijo_Caracteristica();
                Caracteristica.id_enlace_hijo = id_enlace_hijo;
                Caracteristica.caracteristica_validate = listaDT[i];
                Caracteristica.tipo_validate = 4;
                Caracteristica.value = listaDD[i];
                Caracteristica.sysdate = DateTime.Now;
                context.Enlace_Hijo_Caracteristica.Add(Caracteristica);
            }
            context.SaveChanges();
            sTxt = "";
            }
            catch (Exception)
            {
                Console.WriteLine("Error Descripcion basica:" + id_enlace_hijo);
            }

            #endregion

            #region EXTRACT -> Galeria de fotos do piso
            try
            { 
            
            while (!bEndOfThumbs)
            {
                sScript = "CODE:TAG POS=1 TYPE=li ATTR=id:\"thumb" + iThumbs + "\" EXTRACT=HTM" + Environment.NewLine;
                app.iimPlay(sScript);
                sTxt = app.iimGetLastExtract();
                if ((sTxt.IndexOf("#EANF#") >= 0)  || (sTxt.IndexOf("NODATA") >= 0))
                {
                    bEndOfThumbs = true;
                }
                else
                {
                    sTxt = sTxt.Replace("[EXTRACT]", "");
                    htmlDoc.LoadHtml(sTxt);

                    List<string> listaImages = new List<string>();
                    var lis = htmlDoc.DocumentNode.SelectNodes("//li");

                    Enlace_Hijo_Caracteristica Caracteristica = new Enlace_Hijo_Caracteristica();
                    foreach (var input in lis)
                    {

                        sTxt =  input.Attributes["onclick"].Value;
                        sTxt = sTxt.Replace("show_image_detail(this.id,'", "");
                        sTxt = sTxt.Replace("');", "");

                        Caracteristica.id_enlace_hijo = id_enlace_hijo;
                        Caracteristica.caracteristica_validate = "FOTO";
                        Caracteristica.tipo_validate = 1;
                        Caracteristica.value = sTxt;
                        Caracteristica.sysdate = DateTime.Now;

                    }
                    context.Enlace_Hijo_Caracteristica.Add(Caracteristica);
                    context.SaveChanges();
                }
                iThumbs++;

            }
            sTxt = "";
            }
            catch (Exception)
            {
                Console.WriteLine("Error Images:" + id_enlace_hijo);
            }
            #endregion

            #region EXTRACT -> Lista de caracteristicas
            while (!bEnfOfFeatures)
            {
                sScript = "CODE:TAG POS=" + iFeatures + " TYPE=span ATTR=class:\"extra_features_detail extra_features_detail_sel\" EXTRACT=TXT" + Environment.NewLine;
                app.iimPlay(sScript);
                sTxt = app.iimGetLastExtract();
                if (sTxt.IndexOf("#EANF#") >= 0)
                {
                    bEnfOfFeatures = true;
                }
                else
                {
                    sTxt = sTxt.Replace("[EXTRACT]", "");
                    Enlace_Hijo_Caracteristica Caracteristica = new Enlace_Hijo_Caracteristica();
                    Caracteristica.id_enlace_hijo = id_enlace_hijo;
                    Caracteristica.caracteristica_validate = "UTILIDADE";
                    Caracteristica.tipo_validate = 3;
                    Caracteristica.value = sTxt;
                    Caracteristica.sysdate = DateTime.Now;
                    context.Enlace_Hijo_Caracteristica.Add(Caracteristica);
                    
                }
                iFeatures++;
            }
            context.SaveChanges();
            #endregion

            #region EXTRACT -> Telefono
            sScript = "";
            sScript = "CODE: SET !USERAGENT \"Mozilla/5.0 (Linux; U; Android 2.3.3; de-ch; HTC Desire Build/FRF91) AppleWebKit/533.1 (KHTML, like Gecko) Version/4.0 Mobile Safari/533.1\"" + Environment.NewLine;
            sScript += "FILTER TYPE=IMAGES STATUS=ON " + Environment.NewLine;
            sScript += "URL GOTO=" + Hijo.url + Environment.NewLine;
            sScript += "WAIT SECONDS= 1" + Environment.NewLine;
            sScript += "TAG POS=1 TYPE=li ATTR=class:\"yellowButton adInfo_tel\" EXTRACT=TXT" + Environment.NewLine;
            sScript += "SET !USERAGENT \"\"" + Environment.NewLine;
            status = app.iimPlay(sScript);
            if (status.ToString() != "sOk")
            {
                erroScrapearPropriedade(id_enlace_hijo, status.ToString());
                this.globalCount = 0;
                controlMemoriaImacros();
                return;  
            }
                
            try { Detalle.telefono = this.cleanString(app.iimGetLastExtract()); }catch { Detalle.telefono = null; }
            #endregion

            #region 5. Lanzamos el proceso que valida las nuevas caracteristicas*/
            context.Database.ExecuteSqlCommand("SP_ENLACE_HIJO_CARACTERISTICA @id_enlace_hijo = {0}", id_enlace_hijo);
            #endregion

            #region 6. ################## Guardando nueva liena ####################
            Detalle.sysdate = DateTime.Now;
            context.Enlace_Hijo_Detalle.Add(Detalle);
            context.SaveChanges();

            /*Actualizando status de la linea de Enlace Hijo*/
            var result = context.Enlace_Hijo.SingleOrDefault(b => b.id_enlace_hijo == id_enlace_hijo);
            if (result != null)
            {
                context.Entry(result).State = System.Data.Entity.EntityState.Modified; 
                result.scrapped_date = DateTime.Now;
                result.error_handler = "OK";
                context.SaveChanges();
            }
            #endregion

        }

        public Nullable <DateTime> formatearFecha(string fecha)
        {

            try { 


                    fecha = fecha.ToUpper();
                   // fecha = fecha.Replace("  ", string.Empty);
                    Regex rgx = new Regex("[^a-zA-Z0-9 :]");
                    fecha = rgx.Replace(fecha, "");
                    fecha = fecha.Trim();

                    string hora = "";
                    string dia = "";
                    string mes = "";
                    string ano = "";

                    string[] split = fecha.Split(' ');
                    if (split.Count() == 2)
                    {
                        hora = split[1];
                        if (fecha.Contains("HOY"))
                        {
                            dia = Convert.ToString(DateTime.Now.Day);
                        }
                        if (fecha.Contains("AYER"))
                        {
                            DateTime date = DateTime.Now.AddDays(-1);
                            dia = date.Day.ToString();
                        }
                        mes = Convert.ToString(DateTime.Now.Month);
                        ano = Convert.ToString(DateTime.Now.Year);
                    }
            
                    if (split.Count() == 3)
                    {
                        dia = split[0];
                        mes = Convert.ToString(convertirMes(split[1]));
                        hora = split[2];

                        if (Convert.ToInt16(mes) > DateTime.Now.Month)
                        {
                            DateTime date = DateTime.Now.AddYears(-1);
                            ano = date.Year.ToString(); 
                        }
                        else
                        {
                            ano = Convert.ToString(DateTime.Now.Year);
                        }

                
                    }


                    fecha = ano + "-" + mes + "-" + dia + " " + hora;
                    return Convert.ToDateTime(fecha);

            }
            catch (Exception)
            {
                return null;
            }

        }

        public string cleanString(string value)
        {
            try
            {
                value = value.Replace("[EXTRACT]", "");
                value = value.Replace("#EANF#", "");
                value = value.Replace("NODATA", "");

                return value;
            }
            catch (Exception)
            {
                return "";
            }

        }

        public int convertirMes(string nombreMes)
        {

            int mes = 0;
            switch (nombreMes)
            {

                case "ENE":
                    mes = 1;
                    break;
                case "FEB":
                    mes = 2;
                    break;
                case "MAR":
                    mes = 3;
                    break;
                case "ABRI":
                    mes = 4;
                    break;
                case "ABR":
                    mes = 4;
                    break;
                case "MAY":
                    mes = 5;
                    break;
                case "JUN":
                    mes = 6;
                    break;
                case "JUL":
                    mes = 7;
                    break;
                case "AGO":
                    mes = 8;
                    break;
                case "SEP":
                    mes = 9;
                    break;
                case "OCT":
                    mes = 10;
                    break;
                case "NOV":
                    mes = 11;
                    break;
                case "DIC":
                    mes = 12;
                    break;
            };

            return mes;

        }

        public void DeletarCaracteristica(int id_enlace_hijo)
        {
            context.Enlace_Hijo_Caracteristica.RemoveRange(context.Enlace_Hijo_Caracteristica.Where(x => x.id_enlace_hijo == id_enlace_hijo));
        }

        public void DeletarDetalle(int id_enlace_hijo)
        {
            context.Enlace_Hijo_Detalle.RemoveRange(context.Enlace_Hijo_Detalle.Where(x => x.id_enlace_hijo == id_enlace_hijo));
        }

        public void erroScrapearPropriedade(int id_enlace_hijo,string errorCode)
        {
            var result = context.Enlace_Hijo.SingleOrDefault(b => b.id_enlace_hijo == id_enlace_hijo);
            if (result != null)
            {
                context.Entry(result).State = System.Data.Entity.EntityState.Modified;
                result.scrapped_date = DateTime.Now;
                result.error_handler = errorCode;
                context.SaveChanges();
            }
        }

    }
}
