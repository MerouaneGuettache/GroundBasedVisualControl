/* ========================================
 *
 * VDAC example 
 *
 * ========================================
*/
#include <project.h>
#include <stdio.h> 
#include <math.h> 

int main()
{
    CyGlobalIntEnable; /* Enable global interrupts. */

    /*Start Components*/ 
    
    ADC_Start(); 
    VDAC_Start(); 
    UART_Start(); 
    
    /*Declare variables*/ 
    uint16 ADC_reading; 
    float voltage; 
    char  output_string[50]; 
    uint8 i = 0; 

    while(1)
    {
        
        for (i = 0; i < 255; i++)
        {
            VDAC_SetValue(i);   //increment VDAC output 
            
            ADC_StartConvert(); // read Vdac output 
            
            ADC_reading = ADC_GetResult16(); 
            
            voltage = ADC_CountsTo_Volts(ADC_reading); 
           
            
            sprintf(output_string, "%9.6f", voltage); 
            UART_PutString(output_string); //print to UART 
            UART_PutString("\r\n");           
            
            CyDelay(250); 
            
        } 
       i = 0; 
    }
}

/* [] END OF FILE */
