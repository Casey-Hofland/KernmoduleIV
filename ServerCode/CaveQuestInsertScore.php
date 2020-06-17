<?php
	include "Connect.php";
	
	$playerName = getOrDefault("playerName");
	$playerName = preg_replace('/[^A-Za-z0-9\-]/', '', $playerName); // Removes special chars.
	$playerName = filter_var($playerName, FILTER_SANITIZE_STRING);
	if(empty($playerName)) 
	{
		echo "Name is invalid." . "<br/>";
		echo json_encode(0);
		return json_encode(0);
	}

	$score = getOrDefault("score");
	if(filter_var($score, FILTER_VALIDATE_INT) === false || $score < 0)
	{
		echo "Score is invalid." . "<br/>";
		echo json_encode(0);
		return json_encode(0);
	}

	$result = query("SELECT id FROM caveQuestPlayer WHERE name = '$playerName'");
	if(mysqli_num_rows($result) == 0)
	{
		query("INSERT INTO caveQuestPlayer (name) VALUES ('$playerName')");
		$result = query("SELECT id FROM caveQuestPlayer WHERE name = '$playerName'");
	}

	$playerID = $result->fetch_assoc()["id"];

	date_default_timezone_set("Europe/Amsterdam");
	$date = date("Y-m-d H:i:s");

	query("INSERT INTO caveQuestScore (playerID, amount, date) VALUES ('$playerID', '$score', '$date')");
	echo json_encode(1);
	return json_encode(1);
?>