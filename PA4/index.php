
<?php

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

//Function to find the closest matching player name 
function findBestMatch($stmt, $input){
	$maxMatchPercentage = 0;
	$bestMatchName = "";

	while($row = $stmt->fetch()) {
		$percent = 0;
		similar_text(strtolower($row[0]), strtolower($input), $percent);
		if($percent > $maxMatchPercentage){
			$maxMatchPercentage = $percent;
			$bestMatchName = $row[0];
		}
	}

	return $bestMatchName;
}

//Handles the case of a misspelled name with no returned data
function handleMisspelling($input){
	try {
		$conn = new PDO('mysql:host=info344user.ctity8mxeijh.us-east-2.rds.amazonaws.com;dbname=CSV_DB', "info344user", "Tejveer18");
		$conn->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);    

		$stmt = $conn->prepare('SELECT Name FROM NBA_TABLE');
		$stmt->execute();

		search(findBestMatch($stmt, $input));

	} catch(PDOException $e) {
		echo 'ERROR: ' . $e->getMessage();
	}

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
		$names[] = $row[0];
		$teams[] = $row[1];
		$gamesPlayed[] = $row[2];
		$PPGs[] = $row[21];
		$ThreePTMs[] = $row[7];
		$REBs[] = $row[15];
		$ASTs[] = $row[16];


		$constructedLink = "https://nba-players.herokuapp.com/players/" . 
		explode(" ", $row[0])[1] . "/" . 
		explode(" ", $row[0])[0]; 
		$imageLinks[] = $constructedLink;

	}

	if(!$resultsFound){
		handleMisspelling($input);
	}

}

//Function to create an SQL query to search for NBA statistics based on an inputted name
function search($input){

	try {
		$conn = new PDO('mysql:host=info344user.ctity8mxeijh.us-east-2.rds.amazonaws.com;dbname=CSV_DB', "info344user", "Tejveer18");
		$conn->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);    

		$stmt = $conn->prepare('SELECT * FROM NBA_TABLE WHERE Name LIKE :search');

		$stmt->execute(array('search' => "%" . $input . "%"));

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

<html lang="en">
<head>
	<title>NBA Search</title>
	<link rel="stylesheet" href="https://maxcdn.bootstrapcdn.com/bootstrap/4.0.0-beta.3/css/bootstrap.min.css" integrity="sha384-Zug+QiDoJOrZ5t4lssLdxGhVrurbmBWopoEl+M6BdEfwnCJZtKxi1KgxUyJq13dy" crossorigin="anonymous">

</head>

<body>


	<form action="index.php" method="get">
		<div style="text-align: center;"> 
			<div style="display: inline-block; width: 50%;">
				<img width='500px' src='/img/search_logo.png' />
				<div class="input-group p-4">
					<input name="search" class="form-control col-12" type="text" placeholder="NBA Player Name">
					<span class="input-group-btn">
						<input type="submit" class="btn btn-info">
					</span>
				</div>
			</div>
		</div>

	</form>

	<div class="card-text container">   
		<table class="table table-striped">
			<thead>
				<tr>
					<th>Name</th>
					<th>Photo</th>
					<th>Team</th>
					<th>Games Played</th>
					<th>PPG</th>
					<th>3PT Made</th>
					<th>Rebounds</th>
					<th>Assists</th>
				</tr>
			</thead>
			<tbody>
				<?php for($i = 0; $i < count($names); $i++){ ?>
				<tr>
					<td><?php echo $names[$i] ?></td>
					<td>
						<img width="125" src= 
						<?php echo getPlayerImage($imageLinks[$i]); ?> />
					</td>
					<td><?php echo $teams[$i] ?></td>
					<td><?php echo $gamesPlayed[$i] ?></td>
					<td><?php echo $PPGs[$i] ?></td>
					<td><?php echo $ThreePTMs[$i] ?></td>
					<td><?php echo $REBs[$i] ?></td>
					<td><?php echo $ASTs[$i] ?></td>
				</tr>
				<?php } ?>
			</tbody>
		</table>
	</div>


</body>
</html>
