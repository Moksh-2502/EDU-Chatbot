class AnswerObstacle {
  constructor(scene, x, y, text, isCorrect) {
    this.scene = scene;
    this.text = text;
    this.isCorrect = isCorrect;
    this.sprite = null;
    this.textObject = null;
    
    // Create the obstacle with appropriate sprite and text
    this.createObstacle(x, y);
  }
  
  createObstacle(x, y) {
    // Choose between missile or box based on isCorrect value
    let texture = this.isCorrect ? 'correct_answer_box' : 'wrong_answer_box';
    
    // Fallback to existing assets if custom assets aren't available yet
    if (!this.scene.textures.exists(texture)) {
      texture = this.isCorrect ? 'coin' : 'missile'; // Use existing assets as fallback
      console.warn(`Asset ${texture} not found, using fallback`);
    }
    
    // Create the physical sprite with half the previous size
    this.sprite = this.scene.physics.add.sprite(x, y, texture).setScale(0.0625); // Changed from 0.125 to 0.0625 (half)
    this.sprite.setDepth(5);
    
    // Make sure the hitbox is appropriate for the size
    this.sprite.setSize(this.sprite.width, this.sprite.height);
    
    // Add text on top of the sprite with bigger font size and black text with white outline
    this.textObject = this.scene.add.text(x, y, this.text, {
      fontSize: '16px', // Increased from 14px to 16px
      fill: '#000000', // Black text (changed from white)
      fontFamily: '"Akaya Telivigala"',
      strokeThickness: 3, // Increased outline thickness
      stroke: '#FFFFFF' // White outline (changed from black)
    }).setDepth(6).setOrigin(0.5);
    
    // If using fallback textures, adjust the appearance
    if (texture === 'coin' || texture === 'missile') {
      // Create a background for text visibility with adjusted size
      const textBg = this.scene.add.graphics();
      textBg.fillStyle(this.isCorrect ? 0x00ff00 : 0xff0000, 0.7);
      textBg.fillRoundedRect(
        x - this.textObject.width/2 - 8, // Maintain padding proportion
        y - this.textObject.height/2 - 5, // Maintain padding proportion
        this.textObject.width + 16, // Maintain padding proportion
        this.textObject.height + 10, // Maintain padding proportion
        5 // Maintain corner radius
      );
      textBg.setDepth(5.5);
      
      // Store the background for cleanup
      this.textBackground = textBg;
    }
  }
  
  update(scrollSpeed) {
    // Move the obstacle with the scroll speed
    this.sprite.x -= scrollSpeed;
    this.textObject.x = this.sprite.x;
    
    // Update text background if it exists
    if (this.textBackground) {
      this.textBackground.x = this.sprite.x - this.textBackground.originX;
      this.textBackground.clear();
      this.textBackground.fillStyle(this.isCorrect ? 0x00ff00 : 0xff0000, 0.7);
      this.textBackground.fillRoundedRect(
        this.sprite.x - this.textObject.width/2 - 8, // Maintain padding proportion
        this.sprite.y - this.textObject.height/2 - 5, // Maintain padding proportion
        this.textObject.width + 16, // Maintain padding proportion
        this.textObject.height + 10, // Maintain padding proportion
        5 // Maintain corner radius
      );
    }
    
    // Check if obstacle is off-screen for cleanup
    if (this.sprite.x < -this.sprite.width) {
      this.destroy();
      return true; // Signal that this obstacle was destroyed
    }
    return false;
  }
  
  destroy() {
    if (this.textBackground) this.textBackground.destroy();
    if (this.textObject) this.textObject.destroy();
    if (this.sprite) this.sprite.destroy();
  }
}

export default AnswerObstacle; 