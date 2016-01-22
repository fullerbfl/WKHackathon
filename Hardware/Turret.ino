/*
 * Turret control for Wolters Kluwer Hackathon Dec 14, 2015
 *
 * Attach :
 *          Spin-up relay to pin 8
 *          Trigger relay to pin 9
 *          Servo to pin 10
 *          
 *          Comms online LED to pin 2
 *          Spinup initiated LED to pin 3
 *          Fire initiated LED to pin 4
 */


#include <Servo.h>

// Servo
Servo servoHorizontal;

// Relays
int spinupPin = 8;
int firePin = 9;

// Status LEDs
int commsLED = 2;
int spinupLED = 3;
int fireLED = 4;

// Communication 
char cmd = 0;
char data = 0;
int cnt = 0;

void setup() {
  Serial.begin(57600);
  while (!Serial) {
    ; // wait for serial port to connect. Needed for native USB port only
  }

  Serial.println("Starting Turret Control System");
  // Attach to servo and initialize each pin
  servoHorizontal.attach(10);
  pinMode(spinupPin, OUTPUT);
  Serial.println("Platform.....success");
  pinMode(firePin, OUTPUT);
  pinMode(commsLED, OUTPUT);
  pinMode(spinupLED, OUTPUT);
  pinMode(fireLED, OUTPUT);


  // Just some fancy stuff with the LEDs to show the board is "online"
  digitalWrite(commsLED, HIGH);
  Serial.println("Comms.....success");
  delay(750);
  digitalWrite(spinupLED, HIGH);
  Serial.println("Spinup.....success");
  delay(750);
  digitalWrite(fireLED, HIGH);
  Serial.print(".");
  delay(750);
  digitalWrite(fireLED, LOW);
  Serial.print(".");
  delay(350);
  digitalWrite(fireLED, HIGH);
  Serial.print(".");
  delay(350);
  digitalWrite(fireLED, LOW);
  Serial.print(".");
  delay(350);
  digitalWrite(fireLED, HIGH);
  Serial.print(".");
  delay(350);
  digitalWrite(fireLED, LOW);
  Serial.print(".");
  delay(1000);

  Serial.println("");
  digitalWrite(spinupLED, LOW);
  digitalWrite(fireLED, LOW);

  Serial.println("System Online.");
}


void loop() 
{

  // Basically, looking for commands to come across the serial connection
  if (Serial.available() > 0)
  {
    char tmp = Serial.read();
    if (cnt == 0 && (tmp == 'M' || tmp == 'S' || tmp == 'F' || tmp == 'Q'))
    {
      // Grab the command
      Serial.print(tmp);
      cmd = tmp;      
      cnt = 1;
    }
   else if (cnt == 1)
    {
      // Grab the value for the command
      Serial.print(tmp);
      data = tmp;
      execute();
      cnt = 0;
    }
  }
}


// Execute the commands received through the Serial connection
// M - Move servo to specific position
// S - Trigger the spinup relay
// F - Trigger the firing relay
// Q - Reset the relays
void execute()
{
  /////////////////////////////////

  if (cmd == 'M')
  {
    int calcPos = ((int)data * 2);
    servoHorizontal.write((int)calcPos);
    delay (10);
  }
  else if (cmd == 'S')
  {
    digitalWrite(spinupLED, HIGH);
    digitalWrite(spinupPin, HIGH);
    delay (10);
  }
  else if (cmd == 'F')
  {
    digitalWrite(fireLED, HIGH);
    digitalWrite(firePin, HIGH);
    delay (10);
  }
  else if (cmd == 'Q')
  {
    digitalWrite(spinupPin, LOW);
    digitalWrite(firePin, LOW);
    digitalWrite(spinupLED, LOW);
    digitalWrite(fireLED, LOW);

  }    
}

