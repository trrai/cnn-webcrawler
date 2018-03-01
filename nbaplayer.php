<?php
class NBAPlayer
{
	public $name;
	public $team;
	public $gp;
	public $ppg;
	public $threes;
	public $reb;
	public $asts;
	public $imgLink;

	function __construct($name, $team, $gp, $ppg, $threes, $reb, $asts, $imgLink) {
			$this->name = $name;
			$this->team = $team;
			$this->gp = $gp;
			$this->ppg = $ppg;
			$this->threes = $threes;
			$this->reb = $reb;
			$this->asts = $asts;
			$this->imgLink = $imgLink;
	}

}
?>
