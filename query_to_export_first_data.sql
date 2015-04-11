CREATE PROCEDURE SP_SEGUNDAMANO_REPORT_UTILIDADE

AS
BEGIN

		declare  @columnas varchar(max)
		declare  @pivot_columns varchar(max)

		/*Pivot con todos las utilidades*/
		set @columnas =
		(
				SELECT  '['+value+'],' + ''
						--'case when MAX(['+value+']) is not null then ''SI'' else ''NO'' end as ['+upper(value)+'],'
				FROM Enlace_Hijo_Caracteristica where id_caracteristica = 11
				group by value
				order by COUNT(*) desc
				for xml path ('')
		)
		set @columnas = SUBSTRING(@columnas,0,LEN(@columnas))


		/*Pivot con todos las utilidades*/
		set @pivot_columns =
		(
				SELECT  'case when MAX(['+value+']) is not null then ''SI'' else ''NO'' end as ['+upper(value)+'], ' + ''
				FROM Enlace_Hijo_Caracteristica where id_caracteristica = 11
				group by value
				order by COUNT(*) desc
				for xml path ('')
		)
		set @pivot_columns = SUBSTRING(@pivot_columns,0,LEN(@pivot_columns))


		exec
		(
		'
			IF OBJECT_ID(''tempdb..##utilidades'') is not null drop table ##utilidades
			SELECT  id_enlace_hijo ,
					'+@pivot_columns+'
					into ##utilidades
			FROM Enlace_Hijo_Caracteristica 
			PIVOT
			(
					max(id_detalle_caracteristica) FOR value IN (
					'+@columnas+'
				)
			)final

			group by id_enlace_hijo
		'
		)
		/*Pivot con todos las utilidades*/


		SELECT     id_property ,
				   publish_date ,
				   --inactive_date ,
				   ed.nombre ,
				   EC4.value as [Calle] ,
				   EC3.value as [CP] ,
				   ed.visitas as [VISITAS] ,
				   ed.precio as [PRECIO],
				   EC1.value as [QTD HAB] ,
				   replace(EC2.value,'m2','') as [SUPERFICIE],
				   fotos.total as [QTD FOTOS] ,
				   utilidades.total as [QTD CARACTERISTICAS] ,
				   uti.*
			       
			   
		FROM	   Enlace_Hijo EH
		inner join Enlace_Hijo_Detalle ED
		on		   EH.id_enlace_hijo = ED.id_enlace_hijo

				   /*Nº HAB*/	
		left join  Enlace_Hijo_Caracteristica EC1
		on		   eh.id_enlace_hijo = EC1.id_enlace_hijo and ec1.id_caracteristica = 7  

				   /*SUPERFICIE*/	
		left join  Enlace_Hijo_Caracteristica EC2
		on		   eh.id_enlace_hijo = EC2.id_enlace_hijo and EC2.id_caracteristica = 10  

				   /*CP*/	
		left join  Enlace_Hijo_Caracteristica EC3
		on		   eh.id_enlace_hijo = EC3.id_enlace_hijo and EC3.id_caracteristica = 4  

				   /*Calle*/	
		left join  Enlace_Hijo_Caracteristica EC4
		on		   eh.id_enlace_hijo = EC4.id_enlace_hijo and EC4.id_caracteristica = 1 
				
				   /*Foto*/
		LEFT JOIN (
					SELECT id_enlace_hijo , COUNT(1) total FROM Enlace_Hijo_Caracteristica WHERE id_caracteristica = 6
					group by id_enlace_hijo
				   )fotos
		on		   eh.id_enlace_hijo = fotos.id_enlace_hijo	


		LEFT JOIN (
					SELECT id_enlace_hijo , COUNT(1) total FROM Enlace_Hijo_Caracteristica WHERE id_caracteristica = 11
					group by id_enlace_hijo
				   )utilidades
		on		   eh.id_enlace_hijo = utilidades.id_enlace_hijo		   

		left join  ##utilidades uti
		on		   eh.id_enlace_hijo = uti.id_enlace_hijo
		
END




SP_SEGUNDAMANO_REPORT_UTILIDADE