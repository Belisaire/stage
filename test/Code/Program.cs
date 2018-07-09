using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Linq;
using System.Collections;
using System.Xml;

namespace TransactSqlScriptDomTest
{
    class Program
    {
        static string path_queries = @"..\..\..\Ressources\queries.txt";
        static string path_script = @"..\..\..\Ressources\view_script.txt";
        static string path_myf = @"..\..\..\Ressources\myf.txt";

        static Dictionary<String, List<String>> CreateDictionnary(string text2)
        {
            Dictionary<String, List<String>> PhysicalTableList = new Dictionary<String, List<String>>();
            string[] views = Regex.Split(text2, "________________________________________");
            String sDir = @"F:\storage\sqlshare_data_release1\sqlshare_data_release1\data";
            foreach (String view in views)
            {
                Console.WriteLine("En cours ...");
                Regex rx = new Regex(@"\(\[.*\]\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                Regex rx2 = new Regex(@"\[[^\[\]\(\)]*\]\.\[[^\[\]]*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                // Find matches.
                String matchText = "";
                String matchText2 = "";
                MatchCollection matches = rx.Matches(view);
                MatchCollection matches2 = rx2.Matches(view);
                if (matches.Count > 0)
                {
                    matchText = matches[0].Groups[0].Value;
                }
                if (matches2.Count > 0)
                {
                    matchText2 = matches2[0].Groups[0].Value;
                }
                if (!PhysicalTableList.ContainsKey(matchText2))
                {
                    using (StreamWriter sw = File.AppendText(path_myf))
                    {

                        sw.WriteLine(matchText2 + " " + matchText.Replace("(", "").Replace(")", ""));

                    }
                    //Console.WriteLine("le match est : " + matchText.Replace("(", "").Replace(")", "") + ", pour la vue nommée : " + matchText2);
                    PhysicalTableList.Add(matchText2, new List<String>(matchText.Replace("(", "").Replace(")", "").Split(',')));
                }
            }
            try
            {
                string firstLine;
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    foreach (string f in Directory.GetFiles(d))
                    {
                        using (StreamReader reader = new StreamReader(f))
                        {
                            firstLine = reader.ReadLine() ?? "";
                        }
                        string[] listIdentifier = f.Split('\\');
                        //Avant-dernière value du chemin de dossier pour récupérer le nom d'utilisateur
                        int pos1 = listIdentifier.Count() - 2;
                        //dernière value .. 
                        int pos2 = listIdentifier.Count() - 1;
                        //Console.WriteLine("[" + listIdentifier[pos1] + "].[" + listIdentifier[pos2] + "] = " + firstLine);
                        PhysicalTableList.Add("[" + listIdentifier[pos1] + "].[" + listIdentifier[pos2] + "]", new List<String>(firstLine.Split(',')));
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return PhysicalTableList;
        }


        static void Main(string[] args)
        {

            /*Lire fichier*/
            // File.Delete(@"C:\Users\Yann\Desktop\test.xml");
            if (!File.Exists(path_script))
            {
                string text = System.IO.File.ReadAllText(path_queries);
                string[] queries = text.Split("________________________________________");
                /*Initialisation parser*/
                var parser = new TSql130Parser(false);
                /*Stocke les erreurs liés à la lecture du parser*/
                IList<ParseError> errors;
                /*Iniatilisation*/
                MyVisitor myvisitor = new MyVisitor();
                VisitorCommonTable myVisitorCommonTable = new VisitorCommonTable();
                myvisitor.PhysicalTableList = CreateDictionnary(System.IO.File.ReadAllText(path_script));
                int i = 0;
                foreach (String query in queries)
                {
                    myVisitorCommonTable.id_requete_courante = i;
                    var fragment = parser.Parse(new StringReader(query), out errors);
                    fragment.Accept(myVisitorCommonTable);
                    i = i + 1;
                }
                myvisitor.dictTableWith = myVisitorCommonTable.dict;

                i = 0;
                /*Pour chaque requête présent dans le fichier, on l'analyse*/
                foreach (var query in queries)
                {
                    /*Sépare chaque query et leur résultat par "______________" pour le fichier de sorti*/
                    myvisitor.save(query, i.ToString());
                    //Console.WriteLine(Environment.NewLine+"_____________________________"+query);
                    /*Enregistre la query en cours*/
                    TSqlFragment fragment = parser.Parse(new StringReader(query), out errors);
                    fragment.Accept(myvisitor);
                    myvisitor.Add();
                    i++;
                    Console.WriteLine(i);
                }
                myvisitor.Imprime();
            }
            else
            {

                /*jesus fucking christ*/
                string text2 = System.IO.File.ReadAllText(path_script);
                Console.WriteLine("Création Dictionnaire...");
                Dictionary<String, List<String>> PhysicalTableList = CreateDictionnary(text2);
                Console.WriteLine("Fin création dictionnaire");
                PreTraitement preTraitement = new PreTraitement(PhysicalTableList);
                preTraitement.Process();
            }

        }
    }

}
