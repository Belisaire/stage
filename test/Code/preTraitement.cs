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
    Dictionary<String, List<String>> PhysicalTableList = new Dictionary<String, List<String>>();
    private XmlDocument doc;

    private string id;
    private string request;
    private int i = 0;
    private int j = 0;
    readonly string path_file = @"..\..\..\..\..\..\test.xml";
    readonly string path_erreur = @"..\..\..\..\..\..\erreur.txt";

    public PreTraitement(Dictionary<String, List<String>> PhysicalTableList)
    {
        this.projections = new List<string>();
        this.fromClauses = new List<string>();
        this.PhysicalTableList = PhysicalTableList;
    }

    public void Affiche()
    {
        foreach (KeyValuePair<String, List<String>> entry in PhysicalTableList)
        {
            Console.WriteLine(entry.Key);
            foreach (string lol in entry.Value)
            {
                Console.WriteLine(lol);
            }

            Console.ReadLine();
        }
    }

    public void Process()
    {
        if (File.Exists(path_erreur))
            File.Delete(path_erreur);
        /*Chargement du fichier test crée plus hut*/
        XmlDocument xmldoc = new XmlDocument();
        xmldoc.Load(path_file);
        /*On récupère les éléments requête*/
        XmlNodeList donnees = xmldoc.GetElementsByTagName("requête");
        List<string> selection = new List<string>();
        List<string> aggregates = new List<string>();
        try
        {
            /*Pour chaque requête contenu dans donnees*/
            foreach (XmlNode data in donnees)
            {
                /*On récupère */
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
                tmp = data.SelectSingleNode("aggregates");
                if (tmp != null)
                    if (tmp.SelectNodes("aggregates") != null)
                        foreach (XmlNode projection in tmp.SelectNodes("aggregate"))
                            aggregates.Add(projection.InnerText);
                tmp = data.SelectSingleNode("selections");
                if (tmp != null)
                    if (tmp.SelectNodes("selections") != null)
                        foreach (XmlNode projection in tmp.SelectNodes("selection"))
                            selection.Add(projection.InnerText);


                id = data.SelectSingleNode("id").InnerText;
                request = data.SelectSingleNode("request").InnerText;
                string nouvelleProjection = Modify();
                Ecrire(nouvelleProjection, fromClauses, request, id, selection, aggregates);
                projections.Clear();
                fromClauses.Clear();
                selection.Clear();
                aggregates.Clear();
                if (nouvelleProjection.Equals(""))
                    Erreur(request);

            }

            doc.Save(@"..\..\..\..\..\..\traitement.xml");
        }
        catch (Exception e) { Console.WriteLine(e.Message); Console.ReadLine(); }
    }
    public void Ecrire(string projections, List<string> fromClauses, string requete, string id, List<string> selection, List<string> aggregate)
    {
        XmlElement nouveau = null;
        XmlElement noeud = null;
        XmlElement sousn = null;
        try
        {
            if (doc == null)
            {
                doc = new XmlDocument();
                XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
                doc.AppendChild(dec);
                XmlElement root = doc.CreateElement("requêtes");
                doc.AppendChild(root);
            }
            /*On crée l'élément requête qui contiendra tout les éléments*/
            nouveau = doc.CreateElement("requête");
            /*Ajout de l'id*/
            noeud = doc.CreateElement("id");
            noeud.InnerText = id;
            nouveau.AppendChild(noeud);
            /*Ajout de la requête*/
            noeud = doc.CreateElement("request");
            noeud.InnerText = requete;
            nouveau.AppendChild(noeud);
            /*Pour chaque projection trouvvé, on crée un élément correspond*/
            noeud = doc.CreateElement("projections");
            foreach (var element in projections.Split("|"))
            {
                sousn = doc.CreateElement("projection");
                sousn.InnerText = element;
                noeud.AppendChild(sousn);
            }
            nouveau.AppendChild(noeud);
            /*On crée un noeud "selections" qui contiendra toute les sélections trouvées*/
            noeud = doc.CreateElement("selections");
            if (!selection.Equals(""))
                foreach (var select in selection)
                {

                    if (!select.Equals("") || !select.Equals(" "))
                    {
                        sousn = doc.CreateElement("selection");
                        sousn.InnerText = select;
                        noeud.AppendChild(sousn);

                    }
                }
            /*Permet d'ajouter le sous-noeud au doc*/
            nouveau.AppendChild(noeud);
            /*même principe que pour projection*/
            noeud = doc.CreateElement("aggregates");
            foreach (var agg in aggregate)
            {
                if (!agg.Equals(""))
                {

                    sousn = doc.CreateElement("aggregate");
                    sousn.InnerText = agg;
                    noeud.AppendChild(sousn);
                }
            }
            nouveau.AppendChild(noeud);
            /*même principe que pour projection*/
            noeud = doc.CreateElement("fromClause");
            foreach (var from in fromClauses)
            {
                if (!from.Equals(""))
                {
                    sousn = doc.CreateElement("from");
                    sousn.InnerText = from;
                    noeud.AppendChild(sousn);
                }
            }
            nouveau.AppendChild(noeud);
            /*on ajoute le noeud requête en entier au doc*/
            XmlElement el = (XmlElement)doc.SelectSingleNode("requêtes");
            el.AppendChild(nouveau);
        }

        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }
    public string Modify()
    {
        Console.WriteLine(request);
        string tmp = "";
        if (projections.Count == 0)
            Console.WriteLine(++i);
        else if (fromClauses.Count == 0)
            Console.WriteLine(++i);

        else if (fromClauses.Count == 1)
        {

            foreach (string projection in this.projections)
            {
                Console.WriteLine(++i);
                if (!projection.Equals(""))
                {
                    tmp += "|" + fromClauses[0] + "." + Improve(projection);
                }
            }

        }
        else
        {
            string identifier;

            foreach (var projection in projections)
            {
                identifier = HasIdentifier(projection);
                if (identifier != "")
                {
                    ++i;
                    if (!identifier.Equals(""))
                    {
                        // Console.WriteLine(projection);
                        tmp += "|" + ReplaceIdentifier(request, identifier, projection);
                    }
                }
                else if (projection.ToLower().Contains("count(*)"))
                {
                    Console.WriteLine(++i);
                    string allClause = "";
                    foreach (string from in fromClauses)
                    {
                        allClause += '(' + (new Regex(@"^ *| *$|\r\n *")).Replace(Improve(from), "") + ").";
                        if (request.Contains("UNION") || request.Contains("union"))
                            break;
                    }
                    tmp += "|" + allClause + Improve(projection);
                    Console.WriteLine(tmp);
                }
                else if (request.Contains("UNION ") || request.Contains("union"))
                {
                    ++i;
                    tmp += "|" + fromClauses[0] + ".(" + projection + ")";
                }
                else if (projection.ToLower().Contains("case "))
                {
                    ++i;
                    tmp += "|";
                }
                else if (new Regex(@"\w*\(\w*(.[\w,';' /]*)?\)").Match(projection).Success)
                {
                    foreach (var from in fromClauses)
                    {
                        ++i;
                        tmp += '|' + from + '.' + '(' + projection + ')';
                    }

                }
                else if (new Regex(@"\w*[+-/]\w*").Match(projection).Success)
                {
                    foreach (var from in fromClauses)
                    {
                        ++i;
                        tmp += '|' + from + '.' + '(' + projection + ')';
                    }
                }
                else
                {
                    foreach (string from in fromClauses)
                    {
                        if (RechercheDictionnaire(Improve(projection), from))
                        {
                            tmp += "|" + from + "." + Improve(projection);
                            ++i;
                        }
                    }
                }



            }
        }
        return tmp.Substring(tmp.IndexOf("|") + 1, tmp.Length - tmp.IndexOf("|") - 1);
    }
    public void Erreur(string request)
    {

        string text = "";
        if (!File.Exists(path_erreur))
        {
            StreamWriter file = new StreamWriter(path_erreur);
            file.Write("_______________________" + Environment.NewLine + request + Environment.NewLine);
            file.Close();
        }
        else
        {
            text = System.IO.File.ReadAllText(path_erreur);
            text += "_______________________" + Environment.NewLine + request + Environment.NewLine;
            System.IO.File.WriteAllText(path_erreur, text);

        }
    }

    public string Improve(string test)
    {
        /*si on trouve une projection du type : max(x) as y, on supprime le as y*/

        /*Permet de trouver le as y*/
        Regex regex = new Regex(@" *(as|AS) +[\w.,-\[\]]+");
        /*supprime le as s'il existe*/

        if (regex.Match(test).Success)
            test = regex.Replace(test, "");

        /*Si projection du type : max(x)/y => ( max(x)/y ) 
         Pour cela on regarde la position de la dernière parenthèse, si elle est avant la position du '/'
         Alors on entoure la projection de ( )*/

        if (test.LastIndexOf(")") < test.LastIndexOf("/") || (test.ToLower().Contains("case") && test.ToLower().Contains("when")))
            test = "(" + test + ")";

        /*Si il y a un identifier on le supprime ex : t1.species => species*/
        if (HasIdentifier(test) != "")
            test = test.Substring(test.IndexOf('.') + 1, test.Length - test.IndexOf('.') - 1);

        /*On retourne la projection traitée*/
        return test;
    }
    public string HasIdentifier(string test)
    {
        Regex regex = new Regex(@"^\w+\.\w+|^\w+\.\*|\+ \w+\.\w+");
        Match match = regex.Match(test);
        string tempo;
        string results = "";
        while (match.Success)
        {
            tempo = match.Value.Substring(match.Value.IndexOf('.') + 1, match.Value.Length - match.Value.IndexOf('.') - 1);
            if (tempo.Length > 1 || tempo.Equals('*'))
                if (!results.Contains(tempo.ToLower()))
                    results += "|" + test.ToLower().Substring(0, test.IndexOf('.')).Replace("+", "");
            test = test.Substring(0, match.Index) + " " + test.Substring(match.Index + match.Length, test.Length - match.Index - match.Length);
            match = regex.Match(test);
        }
        if (!results.Equals(""))
        {
            results = (new Regex(@"\s+")).Replace(results, "");
            results = results.Substring(results.IndexOf("|") + 1, results.Length - results.IndexOf("|") - 1);
        }
        return results;
    }

    public string ReplaceIdentifier(string request, string identifier, string projection)
    {
        Regex regex = new Regex(@"(from|join)[\s+]*\[[\w]+\].\[[\w+ .]*\] *\w+");
        Match match = regex.Match(request.ToLower());
        string results = "";
        string tmp = "";
        while (match.Success)
        {
            results += request.Substring(match.Index + 4, match.Length - 4) + "|";
            request = request.Substring(0, match.Index) + Environment.NewLine + request.Substring(match.Index + match.Length, request.Length - match.Index - match.Length);
            match = regex.Match(request.ToLower());
        }
        //Console.WriteLine(results);
        foreach (string identi in identifier.Split('|'))
        {
            foreach (string result in results.Split('|'))
            {

                string identmp = (new Regex(@"\] *\w+")).Match(result).Value.Replace("]", "");
                identmp = (new Regex(@"\s+")).Replace(identmp, "");
                // Console.WriteLine(identmp);
                if (identmp.ToLower().Equals(identi) && !result.Equals(""))
                {
                    if (result.Contains("]"))
                    {
                        if (identi.Length == 1)
                        {
                            identmp = identi;
                            if (tmp.Equals(""))
                                tmp = projection.ToLower();
                        }
                        else if (tmp.Equals(""))
                            tmp = projection;
                        tmp = tmp.Replace(identmp + ".", (new Regex(@"\] *[\w]+").Replace(result, "]")) + ".");
                        tmp = (new Regex(@"\s+")).Replace(tmp, "");
                        //  Console.WriteLine(++i);
                        //  Console.WriteLine(projection);
                    }
                    //  
                }
            }
        }
        // Console.WriteLine(tmp);
        return tmp;
    }

    public Boolean RechercheDictionnaire(string projection, string from)
    {
        List<string> tables;
        Regex lol = new Regex(@"^[\s+]*");
        from = lol.Replace(from, "");
        ///   Console.WriteLine(projection);
        //   Console.WriteLine(from);
        //  Console.ReadLine();
        if (PhysicalTableList.TryGetValue(from, out tables))
            foreach (string table in tables)
            {
                if (table != null)
                {
                    if ((new Regex(@"\[|\]")).Replace(table, "").ToLower().Equals((new Regex(@"\[|\]")).Replace(projection.ToLower(), "")))
                    {
                        return true;

                    }

                }

            }
        return false;
    }
}