<?php
	include "Connect.php";
	
	echo "{ \"highscores\": [";
	$result = query("SELECT player.name, MAX(score.amount) as highscore FROM caveQuestScore score LEFT JOIN caveQuestPlayer player ON (player.id = score.playerID) GROUP BY score.playerID ORDER BY highscore DESC");

	$row = $result->fetch_assoc();
	while($row)
	{
		echo json_encode($row);
		if($row = $result->fetch_assoc())
		{
			echo ", ";
		}
	}
	echo "] }";
?>