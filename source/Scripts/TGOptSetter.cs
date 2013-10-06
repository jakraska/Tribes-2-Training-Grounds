new ScriptObject(IGOptions)
{
   class = IGOptions;
};
function IGOptions::Start(%client, %array)
{
   %client.inOptions = true;
   %client.optDispStat = 0;
   %client.optArray = %array;
   
   %msgfunction = "%msg = $" @ %array @ "::msg[0];";
   eval(%msgfunction);
   
   centerPrint( %client, %msg, 60, 3 );

}
function IGOptions::DisplayCycle(%client)
{
   %array = %client.optArray;
   %countfunction = "%count = $" @ %array @ "::count;";
   eval(%countfunction);
   
   if(%client.optDispStat >= %count)
   {
      %client.optDispStat = 1;

      %msgfunction = "%msg = $" @ %array @ "::msg[1];";
      eval(%msgfunction);
      
      centerPrint( %client, %msg, 60, 3 );
   }
   else
   {
      %client.optDispStat++;
      
      %msgfunction = "%msg = $" @ %array @ "::msg[" @ %client.optDispStat @ "];";
      eval(%msgfunction);
      
      centerPrint( %client, %msg, 60, 3 );
   
   }
}

function IGOptions::End(%client, %canceled)
{
   //if still in intro, don't do anything
   if(%client.optDispStat == 0)
      return;
      

   clearCenterPrint( %client );

   %array = %client.optArray;
   
   if(!%canceled)
   {
      %namefunction = "%Optname = $" @ %array @ "::OptName;";
      eval(%namefunction);
   
      %valfunction = "%client." @ %Optname @ " = $" @ %array @ "::value[" @ %client.optDispStat @ "];";
      eval(%valfunction);
   
      %endfunct = "%end = $" @ %array @ "::EndFunction;";
      eval(%endfunct);
      eval(%end);
   }


   %client.optDispStat = "";
   %client.optArray = "";
   %client.inOptions = false;
   

}

////////////////////////////////////////////////////////////////////////////////
//                            Sniping Range Options                           //
////////////////////////////////////////////////////////////////////////////////
$Sniper::optName = "SnipingDiff";
$Sniper::EndFunction = "SnipingPad::EndofOptions(%client);";
$Sniper::Count = 3;
$Sniper::msg[0] = "Please choose your difficulty level.  Press your JET key to cycle through the levels and your FIRE key to select.  After your difficulty has been selected, press your JET key to toss dummies.  Press JUMP to exit.";

$Sniper::msg[1] = "Easy";
$Sniper::value[1] = "10";

$Sniper::msg[2] = "Medium";
$Sniper::value[2] = "50";

$Sniper::msg[3] = "Difficult";
$Sniper::value[3] = "110";
////////////////////////////////////////////////////////////////////////////////
//                                 Race Options                               //
////////////////////////////////////////////////////////////////////////////////
$Race::optName = "RaceWait";
$Race::EndFunction = "RaceInitiatePad::EndofOptions(%client);";
$Race::Count = 3;
$Race::msg[0] = "Welcome to The Race.  Please select your starting options.  Press your JET key to Cycle through the options and your FIRE key to select.";

$Race::msg[1] = "Wait 60 for more competitors";
$Race::value[1] = "60";

$Race::msg[2] = "Wait 30 for more competitors";
$Race::value[2] = "30";

$Race::msg[3] = "Start race";
$Race::value[3] = "0";


