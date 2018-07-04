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

public class PreTraitement
{
    private List<string> projections;
    private List<string> fromClauses;
    private string id;
    private string request;
    private int i = 0;
    readonly string path_file = @"C:\Users\Yann\Desktop\test.xml";

    public PreTraitement()
    {
        this.projections = new List<string>();
        this.fromClauses = new List<string>();
    }

    public void Process()
    {
        XmlDocument xmldoc = new XmlDocument();
        xmldoc.Load(path_file);
        XmlNodeList donnees = xmldoc.GetElementsByTagName("requête");
        try
        {
            foreach (XmlNode data in donnees)
            {
                XmlNode tmp = data.SelectSingleNode("fromClause");
                if (tmp != null)
                    if (tmp.SelectNodes("from") != null)
                        foreach (XmlNode from in tmp.SelectNodes("from"))
                            this.fromClauses.Add(from.InnerText);
                tmp = data.SelectSingleNode("projections");
                if (tmp != null)
                    if (tmp.SelectNodes("from") != null)
                        foreach (XmlNode projection in tmp.SelectNodes("projection"))
                            this.projections.Add(projection.InnerText);


                id = data.SelectSingleNode("id").InnerText;
                request = data.SelectSingleNode("request").InnerText;
                Modify();
                projections.Clear();
                fromClauses.Clear();

            }
            Console.ReadLine();
        }
        catch (Exception e) { Console.WriteLine(e.Message); }
    }

    public void Modify()
    {
        string tmp;
        if (fromClauses.Count == 0)
            // Console.WriteLine(++i); 
            ;
        else if (fromClauses.Count == 1)
        {
            // Console.WriteLine(++i);
            foreach (string projection in this.projections)
            {

                if (!projection.Equals(""))
                {
                    tmp = Improve(projection);

                    //Console.WriteLine(fromClauses[0] + "." + tmp + Environment.NewLine);

                }
            }

        }
        else
        {
            if(request.ToLower().Contains("join "))
            {
                Console.WriteLine(request);
                foreach(var projection in projections)
                {
                    Console.WriteLine(projection);
                }
                Console.ReadLine();
            }
        }

    }

    public string Improve(string test)
    {
        /*si on trouve une projection du type : max(x) as y, on supprime le as y*/

        /*Permet de trouver le as y*/
        Regex regex = new Regex(@" *as|AS [A-z0-9.,-]+");
        /*supprime le as s'il existe*/

        if (regex.Match(test).Success)
            test = regex.Replace(test, "");

        /*Si projection du type : max(x)/y => ( max(x)/y ) 
         Pour cela on regarde la position de la dernière parenthèse, si elle est avant la position du '/'
         Alors on entoure la projection de ( )*/

        if (test.LastIndexOf(")") < test.LastIndexOf("/") || (test.ToLower().Contains("case") && test.ToLower().Contains("when")))
            test = "(" + test + ")";

        /*Si il y a un identifier on le supprime ex : t1.species => species*/
        if (HasIdentifier(test))
            test = test.Substring(test.IndexOf('.') + 1, test.Length - test.IndexOf('.') - 1);

        /*On retourne la projection traitée*/
        return test;
    }
    public Boolean HasIdentifier(string test)
    {
        Regex regex = new Regex(@"^\w+\.\w+");
        if (regex.Match(test).Success)
            return true;

        return false;
    }

}
