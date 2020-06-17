<?php
    $db_user = '********';
    $db_pass = '********';
    $db_host = '********';
    $db_name = '********';

    /* Open a connection */
    $mysqli = new mysqli("$db_host","$db_user","$db_pass","$db_name");

    /* check connection */
    if ($mysqli->connect_errno) 
    {
        echo "Failed to connect to MySQL: (" . $mysqli->connect_errno() . ") " . $mysqli->connect_error();
        exit();
    }

    function query($clause) 
    {
        if (!($result = $GLOBALS['mysqli']->query($clause))) 
        {
            die("Error (" . $GLOBALS['mysqli']->errno . ") " . $GLOBALS['mysqli']->error);
	    }

        return $result;
    }

    function getOrDefault($key) 
    {
	    if(isset($_GET[$key]))
	    {
            return $_GET[$key];
	    }

	    return NULL;
    }

    function sessionOrDefault($key)
    {
        if(isset($_SESSION) && isset($_SESSION[$key]))
	    {
            return $_SESSION[$key];
	    }

	    return NULL;
    }
?>