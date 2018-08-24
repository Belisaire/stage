# Script c# de transformation de requêtes SQL (la branche la plus à jour est latest_version-2)

Dans le cadre de notre stage de master Yann et moi (willeme) avont développé un script c# de transfromation de données. Il prend en input des requêtes SQL écritent dans le dialect SQL Server puis les transforment en quadruplet : 
 - Un ensemble de projections
 - Un ensemble de séléctions
 - Un ensemble de fonctions d'aggrégations
 - Un ensemble de tables

Par exemple, pour un cas simple voici l'abstraction en quadruplet de la requête que l'on obtient : 

![cas_simple](https://user-images.githubusercontent.com/15943103/44573805-eb01fe00-a787-11e8-90f3-04e222be47ff.png)

Pour réaliser cette abstractions, nous avons utilisé un parser SQL : MSDN TsqlParser et les requêtes SQL en entrée sont issues d'une application SQL-as-a-service qui a gracieusement partagée ses données (cf : https://homes.cs.washington.edu/~billhowe/publications/pdfs/jain2016sqlshare.pdf).


# Output

En sortie de ce script, on obtient un ficher XML ayant découpé l'ensemble des requêtes en explorations et en transformant ces dernières en Quadruplet comme décrit ci-dessus.


![tree_xml](https://user-images.githubusercontent.com/15943103/44581734-32e04f80-a79f-11e8-82f6-e1ff3587da6e.png)

Le fichier est composé d'une balise racine "requests" qui contient toutes les explorations. Dans chaque exploration on retrouve les requêtes qui lui sont associées. Enfin dans la balise requête on retrouve le formatage en quadruplet ainsi que l'id et le texte de la requête.  
