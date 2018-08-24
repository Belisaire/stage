# Script c# de transformation de requêtes SQL

Dans le cadre de notre stage de master Yann et moi (willeme) avont développé un script c# de transfromation de données. Il prend en input des requêtes SQL écritent dans le dialect SQL Server puis les transforment en quadruplet : 
 - Un ensemble de projections
 - Un ensemble de séléctions
 - Un ensemble de fonctions d'aggrégations
 - Un ensemble de tables

Par exemple, pour un cas simple voici l'abstraction en quadruplet de la requête que l'on obtient : 

![cas_simple](https://user-images.githubusercontent.com/15943103/44573805-eb01fe00-a787-11e8-90f3-04e222be47ff.png)

Pour réaliser cette abstractions, nous avons utilisé un parser SQL : MSDN TsqlParser et les données en entrées sont issues d'une application SQL-as-a-service qui a gracieusement partagée ses données (cf : https://homes.cs.washington.edu/~billhowe/publications/pdfs/jain2016sqlshare.pdf).


# Output

En sortie de ce script, on obtient un ficher XML ayant découpé l'ensemble des requêtes en explorations et en transformant ces dernières en Quadruplet comme décrit ci-dessus.
