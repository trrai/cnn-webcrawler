
<?php

include_once('nbaplayer.php');

//Arrays that will be filled with data after a request is sent
$names = []; //player names
$teams = []; //player teams
$gamesPlayed =[]; //number of games played
$PPGs = []; //points per game
$ThreePTMs = []; //Three pointers made
$REBs = []; //Total rebounds received
$ASTs = []; //Total assists received
$imageLinks = []; //Links for player images

//If a search GET parameter is passed, search with the input given
if(isset($_GET["search"])){
	search($_GET["search"]);
}

//Function to handle data received from the PDO request and parse it into viewable information
function handleData($stmt, $input){
	global $names;
	global $teams;
	global $gamesPlayed;
	global $PPGs;
	global $ThreePTMs;
	global $REBs;
	global $ASTs;
	global $imageLinks;

	$resultsFound = false;

	while($row = $stmt->fetch()) {
		$resultsFound = true;


		$constructedLink = "https://nba-players.herokuapp.com/players/" .
		explode(" ", $row[0])[1] . "/" .
		explode(" ", $row[0])[0];

		$newPlayer = NEW NBAPlayer($row[0], $row[1], $row[2], $row[21], $row[7], $row[15], $row[16],
		utf8_encode($constructedLink));

	}

	if($resultsFound){
		if(isset($_GET["callback"])){

			header('Content-Type: text/javascript; charset=utf8');
	    header('Access-Control-Allow-Origin: http://www.example.com/');
	    header('Access-Control-Max-Age: 3628800');
	    header('Access-Control-Allow-Methods: GET, POST, PUT, DELETE');

	    $callback = $_GET['callback'];
			echo $callback. '(' . json_encode($newPlayer) . ');';

		}else{

			header('Content-Type: application/json');
			echo json_encode($newPlayer);
		}
	}
}

//Function to create an SQL query to search for NBA statistics based on an inputted name
function search($input){

	try {
		$conn = new PDO('mysql:host=info344user.ctity8mxeijh.us-east-2.rds.amazonaws.com;dbname=CSV_DB', "info344user", "Tejveer18");
		$conn->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);

		$stmt = $conn->prepare('SELECT * FROM NBA_TABLE WHERE Name LIKE :search');

		$stmt->execute(array('search' => $input ));

		handleData($stmt, $input);


	} catch(PDOException $e) {
		echo 'ERROR: ' . $e->getMessage();
	}
}

//Function that returns the appropriate player image for each NBA player and substitutes with a default image if needed
function getPlayerImage($link){
	$headers = get_headers($link);

	if (strpos($headers[9], 'image/') !== false) {
		return $link;
	} else {
		return "https://reputableseomarketing.com/wp-content/uploads/2016/03/profile-no-photo.png";
	}
}

?>
