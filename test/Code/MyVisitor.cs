using System;
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
    class MyVisitor : TSqlFragmentVisitor
    {
        Boolean filtreWith = false;
        private string id = "";
        private string requete = "";
        private string aggregate = "";
        private string fromClause = "";
        private string filtreHaving = "";
        private XmlDocument doc = null;
        int id_requete_courante = -1;
        int id_requete_precedante = -1;
        public String user_courant = "";
        public String user_precedant = "";
        public Dictionary<int, List<TSqlFragment>> dictTableWith = new Dictionary<int, List<TSqlFragment>>();
        public Dictionary<string, List<string>> PhysicalTableList = new Dictionary<string, List<string>>();
        public List<String> previousProjectionList = new List<String>();
        public Dictionary<int, List<String>> ProjectionListPerQuery = new Dictionary<int, List<String>>();
        public List<String> projectionList;
        public int id_explo = 0;
        static string path_myfriend = @"..\..\..\myfriend.txt";
        static string path_explo = @"..\..\..\explo.txt";
        static string path_test = @"..\..\..\..\..\..\test.xml";
        private string selection = "";

        /*In : fragment
         Out: String
         Prends un fragment trouvé par le visitor et retourne un string correspondant à ce fragment*/
        private string GetNodeTokenText(TSqlFragment fragment)
        {
            StringBuilder tokenText = new StringBuilder();
            for (int counter = fragment.FirstTokenIndex; counter <= fragment.LastTokenIndex; counter++)
            {
                tokenText.Append(fragment.ScriptTokenStream[counter].Text);
            }
            return tokenText.ToString();
        }

        /*Stocke la valeur de l'id et le contenu de toute la requête*/
        public void save(string tmp, String i)
        {
            id = i;
            requete += tmp;
        }

        public override void Visit(QuerySpecification node)
        {
            WhereClause lol = node.WhereClause;
            HavingClause xD = node.HavingClause;
            if (lol != null)
            {
                if (!GetNodeTokenText(lol).ToLower().Contains("select"))
                    seperate(GetNodeTokenText(lol).ToLower().Replace("where", ""));
                else
                    seperate(GetNodeTokenText(lol).ToLower().Substring(6, GetNodeTokenText(lol).Length - 6));
            }
            if (xD != null)
            {
                string havingClause = GetNodeTokenText(xD).ToLower().Substring(6, GetNodeTokenText(xD).Length - 6);
                havingClause = havingClause.Replace("\r\n", "");
                Regex rgx = new Regex(@"\s+");
                havingClause = rgx.Replace(havingClause, " ");
                selection += "| " + havingClause;
                filtreHaving = havingClause;
            }

            /* MY BIG PART OF CODE ! mouahaha*/
            id_requete_courante = Int32.Parse(this.id);
            node.ToString();

            bool starExpression = false;

            if (this.id_requete_courante != this.id_requete_precedante)
            {
                projectionList = new List<String>();
                Regex rxUser = new Regex(@"\[[^\[\]\(\)]*\]\.\[[^\[\]\(\)]*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                // Find matches.//
                //Console.WriteLine(this.PhysicalTableList["[1002].[Tokyo_0_merged.csv]"]);
                MatchCollection matchesUser = rxUser.Matches(this.requete);
                if (matchesUser.Count > 0)
                {
                    String matchTexte = matchesUser[0].Groups[0].Value;
                    this.user_courant = matchTexte.Split('.')[0].Replace("[", "").Replace("]", "");

                }
                if (node.SelectElements != null)
                {
                    foreach (TSqlFragment selectElement in node.SelectElements)
                    {
                        if (selectElement is SelectStarExpression)
                        {
                            starExpression = true;
                            if (node.SelectElements.Count > 1)
                            {
                                //Lever une exception qui dit que c'est non géré*

                                break;
                            }
                            else
                            {
                                if (node.FromClause != null)
                                {
                                    foreach (TSqlFragment fromElement in node.FromClause.TableReferences)
                                    {
                                        if (fromElement is NamedTableReference)
                                        {
                                            String namedTableReferenceText = GetNodeTokenText(fromElement);
                                            if (this.dictTableWith.ContainsKey(this.id_requete_courante))
                                            {
                                                foreach (CommonTableExpression commonTableExpression in this.dictTableWith[this.id_requete_courante])
                                                {
                                                    if (commonTableExpression.ExpressionName.Value == namedTableReferenceText)
                                                    {
                                                        if (commonTableExpression.QueryExpression is QuerySpecification)
                                                        {
                                                            QuerySpecification queryUsedForTableDefinition = (QuerySpecification)commonTableExpression.QueryExpression;
                                                            foreach (SelectElement selectElementInQueryUsedForTableDefinition in queryUsedForTableDefinition.SelectElements)
                                                            {
                                                                projectionList.Add(GetNodeTokenText(selectElementInQueryUsedForTableDefinition));
                                                            }
                                                        }

                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Regex rx = new Regex(@"\[.*\]\.\[.*\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                                                // Find matches.
                                                //Console.WriteLine(this.PhysicalTableList["[1002].[Tokyo_0_merged.csv]"]);
                                                String matchText = "";
                                                MatchCollection matches = rx.Matches(namedTableReferenceText);
                                                if (matches.Count > 0)
                                                {
                                                    matchText = matches[0].Groups[0].Value;
                                                }
                                                if (this.PhysicalTableList.ContainsKey(matchText.ToLower()))
                                                {
                                                    foreach (String attribut in this.PhysicalTableList[matchText.ToLower()])
                                                    {
                                                        projectionList.Add(attribut);
                                                    }
                                                }

                                            }
                                        }
                                    }
                                }
                            }
                        }

                    }
                    if (starExpression == false)
                    {
                        foreach (SelectElement selectElement in node.SelectElements)
                        {
                            if (GetNodeTokenText(selectElement).Contains(','))
                            {
                                //  Console.WriteLine(GetNodeTokenText(selectElement));
                            }
                            projectionList.Add(GetNodeTokenText(selectElement));
                        }
                    }
                }

                ProjectionListPerQuery.Add(this.id_requete_courante, projectionList);
                int nombreProjectionsCommunes = 0;
                int nombreProjections = projectionList.Count;
                if (user_precedant.Equals("") || !user_courant.Equals(user_precedant))
                {
                    nombreProjectionsCommunes = 0;
                    if (!user_courant.Equals(user_precedant))
                    {
                        this.id_explo = this.id_explo + 1;
                    }
                }
                else
                {
                    if (user_precedant.Equals(user_courant))
                    {
                        nombreProjectionsCommunes = previousProjectionList.Intersect(projectionList, StringComparer.InvariantCultureIgnoreCase).ToList().Count;
                        //Faire l'intersection
                    }
                }
                using (StreamWriter sw = File.AppendText(path_explo))
                {

                    sw.WriteLine(this.id_requete_courante + ";" + user_courant + ";" + this.id_explo);
                }
                previousProjectionList = projectionList;
                user_precedant = user_courant;

                using (StreamWriter sw = File.AppendText(path_myfriend))
                {

                    sw.Write("\n-------\nProjection pour la requête : " + this.requete);
                    //sw.Write("Projections pour la requête : " + text_requete_courante);
                    // Console.WriteLine(" Sont : ");
                    //sw.Write("\n Sont :");

                    foreach (String projection in projectionList)
                    {
                        //Console.WriteLine(projection);
                        sw.Write("\n" + projection);

                    }
                    sw.Write("\n--------\n");
                }
            }

            this.id_requete_precedante = this.id_requete_courante;

        }
        public override void Visit(DeleteSpecification node)
        {
            WhereClause lol = node.WhereClause;
            if (lol != null)
                if (!GetNodeTokenText(lol).ToLower().Contains("select"))
                    seperate(GetNodeTokenText(lol).ToLower().Replace("where", ""));
                else
                    seperate(GetNodeTokenText(lol).ToLower().Substring(6, GetNodeTokenText(lol).Length - 6));
        }

        /*A chaque join trouvé dans la requête*/
        public override void Visit(QualifiedJoin node)
        {
            /*On récupére l'information en un string pour pouvoir effectuer des opérations dessus
             On enlève toutes les majuscules*/
            string study = GetNodeTokenText(node).ToLower();

            /*Inialisation de la regex pour trouver les sélections, tout ce qu'il y a après le "on" d'un join*/
            Regex regex = new Regex(@"[(/r)\s+]+on[^A-z].*((\s+)*(substring\([A-z.,'\s+\(\)0-9]*\))|(\s+)*[A-z.0-9]*[\s+]*=[\s+]*[A-z_.0-9]*|(\s+)*[A-z.0-9]*[\s+]=[\s+]*\([A-z\s+0-9.=!\(\]]*\)|(\s+)*[A-z.(\s+)]*=[\s+]*\([A-z\s+0-9.\[\]=!><\(\),']*\)|((\s+)*(and|or).*)*)?");
            Match match = regex.Match(study);

            /*Tant qu'on trouve une clause sélection à récupérer*/
            while (match.Success)
            {
                /*On l'extrait de la chaine de caractère*/
                string tmp = study.Substring(match.Index, match.Length);
                /*Si la selection contient un or ou un and, on appelle la méthode seperate qui va séparer les clauses*/
                if (tmp.Contains("or") || tmp.Contains("and"))
                    seperate(tmp);
                else
                {
                    /*Sinon, on enleve toute les informations superflux ex : "on" du join
                     avant de l'ajouter à selection, on regarde si elle y est déjà*/
                    if (!selection.Contains(tmp.Substring(tmp.IndexOf("on ") + 2, tmp.Length - tmp.IndexOf("on ") - 2)))
                        selection += "|" + tmp.Substring(tmp.IndexOf("on ") + 2, tmp.Length - tmp.IndexOf("on ") - 2);
                }
                /*On enlève la selection trouvée de la chaine initiale et on cherche si il en existe une autre*/
                study = study.Substring(0, match.Index) + " " + study.Substring(match.Index + match.Length, study.Length - (match.Index + match.Length));
                match = regex.Match(study);
            }
        }
        /*Recherche les aggregates*/
        public override void Visit(FunctionCall node)
        {
            string study = GetNodeTokenText(node);
            /*FunctionCall récupère toute les fonctions, la regex ci-dessous permet de ne garder que les aggrégations.*/
            Regex isAggregate = new Regex(@"(sum|avg|checksum_agg|count|count_big|grouping|grouping_id|max|min|stdev|stdevp|string_agg|var|varp)[\s+]*\([*_.\(\)""A-z0-9+=<>/\- \[\]]*\)");
            Match match = isAggregate.Match(study.ToLower());
            /*Si on trouve une aggregation, on l'ajoute à aggregate*/
            if (match.Success)
            {
                isAggregate = new Regex(@"[A-z_\(,\s]+(sum|avg|checksum_agg|count|count_big|grouping|grouping_id|max|min|stdev|stdevp|string_agg|var|varp)\([*_.""\(\)\[\]/A-z0-9+=<>\'-]*\)");
                if (!filtreHaving.Contains(study.ToLower()) && !isAggregate.Match(GetNodeTokenText(node).ToLower()).Success)
                    aggregate += " | " + GetNodeTokenText(node);
            }

        }

        public override void Visit(WithCtesAndXmlNamespaces node)
        {
            /*Savoir s'il existe un with*/
            filtreWith = true;
            string fromWith = GetNodeTokenText(node).ToLower();
            /*On récupère la clause qui nous intéresse*/
            Regex regex = new Regex(@"(from[\s+]*(\[[A-z.0-9]*\].\[[A-z.0-9]*.*\][ ]*[A-z0-9]*)([\s+]*,[ ]*\[[0-9A-z.]*\].\[[A-z0-9.]*\][ ]*[A-z0-9]*)?|join[\s+]*\[[A-z .0-9]*\][ ]*[A-z0-9]*|join[\s+]+[\[\]A-z0-9.]*|from[/s+]*[A-z0-9]*\.[A-z0-9]*|from[\s+]*[A-z ]*[ ]*[A-z0-9]|from[\s+]\[[A-z.0-9 ]*\][ ]*[A-z0-9]*)");
            Match match = regex.Match(fromWith);
            fromClause = "";
            /*Tant qu'il y a des from qui nous intéresse*/


            /*on extrait la clause*/
            string tmp = fromWith.Substring(match.Index, match.Length);
            /*on met à jour le node*/
            fromWith = fromWith.Substring(0, match.Index) + " " + fromWith.Substring(match.Index + match.Length, fromWith.Length - match.Index - match.Length);
            /*on supprime les join et from*/
            tmp = (new Regex(@"from |join ")).Replace(tmp, "");
            match = regex.Match(fromWith);
            /*s'il existe plusieurs tables séparées par ,*/
            foreach (var seperate in tmp.Split(","))
            {
                if (!tmp.Equals("") && !fromClause.Contains(seperate) && !selection.Contains(seperate))
                    fromClause += " | " + seperate;
            }

        }


        /*Extrait les FromClause*/
        public override void Visit(FromClause node)
        {

            string from = GetNodeTokenText(node).ToLower();
            /*supprime les possibles commentaires*/
            Regex rgx = new Regex(@"--[A-z0-9\(\)\[\],._!=<>'\s+][^\r\n]*");
            if (rgx.Match(from).Success)
            {
                from = rgx.Replace(from, "");
            }
            /*Regex pour récupérer les tables des From du type : [0123].[exmaple1] t1, [0123][example2] t2
             => [0123].[exmaple1], [0123].[example]t2*/
            rgx = new Regex(@"from[\s+]*\[[A-z0-9]*.\[[A-z0-9.]*\][ ]*[as ]?[A-z0-9]*([\s+]*,[\s+]*\[[A-z0-9]*.\[[A-z0-9.]*\][ ]*[as ]?[A-z0-9]*)+|from *[0-9.A-z]* *, *[0-9A-z]*");
            Match match = rgx.Match(from);
            /*Si on trouve une clause de ce type*/
            if (match.Success)
            {
                /*On l'extrait de la chaine de caractère initiale, tout en enlevant le from (d'où le +4, longueur de la chaine from ou join)*/
                string tmp = from.Substring(match.Index + 4, match.Length - match.Index - 4);
                /*On sépare les tables une à une grâce à la fonction split*/
                string[] seperate = tmp.Split(",");
                foreach (var element in seperate)
                {
                    /*On cherche à sélectionner les parties du type : [0123].[example1] as t1 => ] as t1*/
                    rgx = new Regex(@"\][ ]*[as]?[ ]*[A-z0-9]*[\s+]*(,)*");
                    /*Et on remplace cette partie là par ] => [0123].[example] as t1 => [0123].[example'] as t1' => [0123].[example]*/
                    tmp = rgx.Replace(element, "]");
                    /*Si from vient d'un WITH clause ou qu'elle a déjà été trouvé on ne l'ajoute pas à FromClause*/
                    if (!tmp.Equals("") && !fromClause.Contains(tmp) && !selection.Contains(tmp) && !filtreWith)
                        fromClause += " | " + tmp;
                }
            }
            else
            {
                /*Regex pour récupérer les formes clauses*/
                rgx = new Regex(@"from[\s+]*\[[0-9.A-z]*\].[\s+]*\[[A-z0-9. ,'-]*\]|from[\s+]*[A-z.0-9]+|join[\s+]+\[[A-z0-9]*\].\[[A-z0-9 .-]*\]|join[\s+]+[A-z.]*");
                // Console.WriteLine(from);
                match = rgx.Match(from);
                /*tant qu'il existe une forme clause à récupérer*/
                while (match.Success)
                {
                    /*On l'extrait de from et on la stock dans tmp*/
                    string tmp = from.Substring(match.Index, match.Length);
                    /*on met à jour from sans tmp*/
                    from = from.Substring(0, match.Index) + " " + from.Substring(match.Index + match.Length, from.Length - match.Index - match.Length);
                    /*On regarde s'il existe une autre clause à récupérer*/
                    match = rgx.Match(from);
                    /*on supprime le from ou join*/
                    tmp = (new Regex(@"(from |join[\s+]*)")).Replace(tmp, "");
                    /*s'il y a plusieurs tables ex [896].[lol], [896].[xD]*/
                    if (!tmp.Equals("") && !fromClause.Contains(tmp) && !selection.Contains(tmp) && !filtreWith)
                        fromClause += " | " + tmp;
                }
            }
        }

        /*In : string contenant une sélection avec un or ou and, between ...
         Out : // 
         Ajoute les sélections trouvées dans selection */
        public void seperate(string pat)
        {
            /*On initialise un tableau qui contiendra, possiblement,les selections séparées par or ou and*/

            String[] sep = new String[0];

            /*Pré-Traitement de la whereClause trouvé, supprime tous les sauts de ligne
             ajoute un espace avant et après une parenthèse
             remplace les espaces blancs par un seul espace
             efface les commentaires dans les where ex : 
             where --or
             x=5 +> where x=5
             */

            Regex rgx = new Regex(@"--[A-z0-9\(\)\[\],._!=<>'\s+][^\r\n]*");
            if (rgx.Match(pat).Success)
            {
                pat = rgx.Replace(pat, "");
            }

            pat = pat.Replace("\r\n", "");
            pat = pat.Replace(")", " ) ");
            pat = pat.Replace("(", " ( ");
            rgx = new Regex(@"\s+");
            pat = rgx.Replace(pat, " ");
            /*Si il existe une clause between, on enleve les espaces entre and et le reste pour qu'il ne soit pas traité par la regex plus bas*/
            rgx = new Regex(@"between[\s+][A-z0-9.]*[\s+](and)[\s+][A-z0-9.]*");
            Match match = rgx.Match(pat);
            if (rgx.Match(pat).Success)
            {
                pat = rgx.Replace(pat, match.Value.Replace(" ", ""));
            }
            /*Fin Pré-Traitement*/
            /*On recherche les cas où les or et and sont dans les parenthèses si on en trouve on l'extrait de la chaine*/
            rgx = new Regex(@"[(][a-z0-9\s+=<>\[\]!'_.]*( or | and )[\(a-z0-9\s+=<>\[\]!'_.&\|]*[)]");
            match = rgx.Match(pat);
            if (match.Success)
            {
                /*On le récupére dans une chaine*/
                string a = pat.Substring(match.Index, match.Length);
                /*Si il contient une clause between, on retraite l'information pour faire apparaitre la clause d'origine*/
                if (a.Contains("between"))
                {
                    a = a.Replace("between", " between ");
                    Regex number = new Regex(@"[0-9.]+and[0-9.]+");
                    Match number2 = number.Match(a);
                    if (number2.Success)
                    {
                        string tmp = number2.Value;
                        tmp = tmp.Replace("and", " and ");
                        a = number.Replace(a, tmp);
                    }
                }
                selection += " | " + a;
                pat = pat.Substring(0, match.Index) + " " + pat.Substring(match.Index + match.Length, pat.Length - (match.Index + match.Length));
            }
            /*On split sur or ou and*/
            sep = Regex.Split(pat, @" or | and ");
            string between = "";
            /*Pour chaque clause séparée par or ou and*/
            foreach (var binary in sep)
            {
                /*Si elle contient un between, on retraite le string pour avoir la clause d'origine sans changements*/
                if (binary.Contains("between"))
                {
                    between = binary.Replace("between", " between ");
                    Regex number = new Regex(@"[0-9.]+and[0-9.]+");
                    Match number2 = number.Match(between);
                    if (number2.Success)
                    {
                        string tmp = number2.Value;
                        tmp = tmp.Replace("and", " and ");
                        /*Et on la stocke dans between*/
                        between = number.Replace(binary, tmp);
                    }
                }
                /*Si between est null, =>aucune forme between ... and ... on ajoute les clauses sélections dans sélections*/
                if (between.Equals(""))
                {
                    rgx = new Regex(@"\s+");
                    string tmp = rgx.Replace(binary, " ");
                    if (!tmp.Equals(" ") && !tmp.Equals(" ) "))
                        selection += " | " + binary;
                }
                /*Sinon, on ajoute la clause between ... and ... et on efface le contenu de between*/
                else
                {
                    selection += " | " + between;
                    between = "";
                }
            }

        }


        /*A la fin de chaque traitement de requête*/
        public void Add()
        {
            /*Si le doc n'est pas crée, on le crée, on ajoute le noeud root "requêtes" qui contiendra toute les traitements de chaque requête*/
            if (doc == null)
            {
                doc = new XmlDocument();
                XmlDeclaration dec = doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
                doc.AppendChild(dec);
                XmlElement root = doc.CreateElement("requêtes");
                doc.AppendChild(root);
            }
            /*On supprime le premier | pour éviter de créer des noeuds vides*/
            selection = selection.Substring(selection.IndexOf("|") + 1, selection.Length - selection.IndexOf("|") - 1);
            aggregate = aggregate.Substring(aggregate.IndexOf("|") + 1, aggregate.Length - aggregate.IndexOf("|") - 1);
            fromClause = fromClause.Substring(fromClause.IndexOf("|") + 1, fromClause.Length - fromClause.IndexOf("|") - 1);
            /*On ajoute le tout dans le doc*/
            this.ecrireFichier();

            /*on reset les informations de chaque traitement*/
            id = "";
            requete = "";
            selection = "";
            aggregate = "";
            fromClause = "";
            filtreHaving = "";
            filtreWith = false;
        }
        /*Crée le fichier à partir de doc*/
        public void Imprime()
        {
            doc.Save(path_test);
        }

        public string SupprimeEspace(string test)
        {
            Regex espaceBlanc = new Regex(@"^[\s+]*|[\s+]*$");
            test = espaceBlanc.Replace(test, "");
            return test;
        }

        /*Créer le fichier de sortie, et écrit le contenu de test*/
        public void ecrireFichier()
        {
            XmlElement nouveau = null;
            XmlElement noeud = null;
            XmlElement sousn = null;
            try
            {
                /*On crée l'élément requête qui contiendra tout les éléments*/
                nouveau = doc.CreateElement("requête");
                /*Ajout de l'id*/
                noeud = doc.CreateElement("id");
                noeud.InnerText = id;
                nouveau.AppendChild(noeud);
                /*Ajout de la requête*/
                noeud = doc.CreateElement("request");
                noeud.InnerText = SupprimeEspace(requete);
                nouveau.AppendChild(noeud);
                /*Pour chaque projection trouvvé, on crée un élément correspond*/
                noeud = doc.CreateElement("projections");
                foreach (var element in this.projectionList)
                {
                    sousn = doc.CreateElement("projection");
                    sousn.InnerText = element;
                    noeud.AppendChild(sousn);
                }
                nouveau.AppendChild(noeud);
                /*On crée un noeud "selections" qui contiendra toute les sélections trouvées*/
                noeud = doc.CreateElement("selections");
                if (!selection.Equals(""))
                    foreach (var select in selection.Split("|"))
                    {

                        if (!select.Equals("") || !select.Equals(" "))
                        {
                            sousn = doc.CreateElement("selection");
                            sousn.InnerText = SupprimeEspace(select);
                            noeud.AppendChild(sousn);

                        }
                    }
                /*Permet d'ajouter le sous-noeud au doc*/
                nouveau.AppendChild(noeud);
                /*même principe que pour projection*/
                noeud = doc.CreateElement("aggregates");
                foreach (var agg in aggregate.Split("|"))
                {
                    if (!agg.Equals(""))
                    {

                        sousn = doc.CreateElement("aggregate");
                        sousn.InnerText = SupprimeEspace(agg);
                        noeud.AppendChild(sousn);
                    }
                }
                nouveau.AppendChild(noeud);
                /*même principe que pour projection*/
                noeud = doc.CreateElement("fromClause");
                foreach (var from in fromClause.Split("|"))
                {
                    if (!from.Equals(""))
                    {
                        sousn = doc.CreateElement("from");
                        sousn.InnerText = SupprimeEspace(from);
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
    }
}



